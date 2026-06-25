import __pycsrt as _pyrt
import clr

clr.AddReference('UnityEngine.CoreModule')
clr.AddReference('Better_Scene')
clr.AddReference('Better_Story')

from UnityEngine import WaitForSeconds,Time,GameObject,Quaternion,Vector3
import BetterExperience.PyStory as pystory

import System

pyrt = _pyrt.api


class Expando(object):
    pass
    

def find_gameobject(*args,root=None):
    return pyrt.find_go_by_name(root,args)

def wait_ms(delay):
    return WaitForSeconds(0.001 * delay)

def stop_dialogue():
    return pyrt.stop_dialogue()
    
def overlay_notify(text,duration=15,fade_out=3):
    pyrt.send_notification(text,duration,fade_out)



class Strand:

    def __init__(self,name=None):
        self._impl = pyrt.new_strand()
        if name:
            self._impl.Name = name
        
    def _get_strand(self):
        return self._impl
    
    def can_invoke_immediate(self):
        return pyrt.can_invoke_immediate(self._get_strand())
        
    def invoke_immediate(self,gen):
        if not self.can_invoke_immediate():
            return None
        return self.invoke_next(gen)
        
    def invoke_next(self,fn):
        gen = fn() if callable(fn) else fn
        if gen is None:
            return None
        return pyrt.invoke_next(gen,self._get_strand())
        
    def invoke_later(self,fn):
        gen = fn() if callable(fn) else fn
        if gen is None:
            return None
        return pyrt.invoke_last(gen,self._get_strand())
        
    def callable_immediate(self,fn):
        def delegate():
            return self.invoke_immediate(fn)
        return delegate
        
    def callable_next(self,fn):
        def delegate():
            return self.invoke_next(fn)
        return delegate
        
    def callable_later(self,fn):
        def delegate():
            return self.invoke_later(fn)
        return delegate
        
    @property
    def busy(self):
        return not self.can_invoke_immediate()
        

main_strand = Strand()
main_strand._impl = pyrt.get_main_strand()

#guest character wrapper interface
class Guest:
    def __init__(self):
        self._guest = pyrt.Session.Guest
        self.name = self._guest.Impl.self.nombreCompleto
        self.first_name = self._guest.Impl.self.nombre

        self.stats = Expando()
        self.stats.rage = _pyrt.ai.Anger
        self.stats.pleasure = _pyrt.ai.Pleasure
        self.stats.pain = _pyrt.ai.Pain
        self.stats.consent = _pyrt.ai.Consent
        self._obj = None
        self.interaction_handler = None

    @property
    def game_object(self):
        return self._guest.Impl.gameObject
    
    @property
    def busy(self):
        return pyrt.is_guest_busy()
            
    @property
    def poi(self):
        ctx = pyrt.get_guest_interaction_context()
        return ctx.CurrentPlace.POI.Id
        
    @property
    def posture(self):
        ctx = pyrt.get_guest_interaction_context()
        return ctx.CurrentPosture.PostureId
    
    @property
    def pose(self):
        ctx = pyrt.get_guest_interaction_context()
        return ctx.activePoseName
        
    def go_to_poi(self,poi):
        return pyrt.guest_goto_poi(poi)
    
    def teleport(self,poi):
        return pyrt.guest_teleport_poi(poi)
        
    def dialogue(self,text):
        return pyrt.dialogue(text)
        
    def say(self,text):
        return pyrt.dialogue(self.name,text)
        
    def apply_posture(self,posture_id):
        return pyrt.apply_posture(posture_id)
        
    def play_clip(self,clip_name):
        return pyrt.play_clip(clip_name)
        
    def comment(self,text):
        self._guest.HeadController.Say(text)
        
    def enumerate_genes(self):
        return self._guest.GuestInstance.Pool.GeneFactory.GeneToGroup.Keys
        
    def install_custom_interaction_handler(self):
        self._obj = pyrt.install_character_interaction()
        self._obj.label = self.name
        self._obj.handler = self._local_handler
        
    def _local_handler(self):
        if self.interaction_handler:
            return self.interaction_handler()
            
    def list_clothes(self):
        return pyrt.enumerate_clothes()
        
    def take_off(self,cloth):
        return pyrt.take_off_cloth(cloth.id)
        
    def take_off_slot(self,slot_id):
        for c in self.list_clothes():
            print(c.name,c.slots)
            if slot_id in c.slots:
                self.take_off(c)
                
    def take_off_slots(self,slots):
        for slot in slots:
            self.take_off_slot(slot)
    
    def find_clothes(self,slots,except_slots=[]):
        result = []
        for c in self.list_clothes():
            print(c.name,c.slots)
            if any(slot_id in c.slots for slot_id in slots) and not any(slot_id in c.slots for slot_id in except_slots):
                result.append(c)
        return result
                
    def take_off_slots_sync(self,slots,except_slots=[]):
        for c in self.find_clothes(slots=slots,except_slots=except_slots):
            self.take_off(c)
            
        yield wait_ms(100)        # skip few frames for animation to start
        
        while self.busy:          # wait until animation completion
            yield wait_ms(100)
    
    def set_eyes_expression(self,expression,duration):
        exp = System.Enum.Parse(pystory.EyeExpression,expression)
        pyrt.set_eye_expression(exp,duration)

    def enumerate_transitions(self):
        for i in pyrt.enumerate_transitions():
            yield _InteractionWrapper(i)
    
    def enumerate_gotos(self):
        for i in pyrt.enumerate_gotos():
            yield _InteractionWrapper(i)
            
    def modify_genes(self,callback,appearance=False,personality=False):
        def traverse_genes(collection):
            changes = []
            for c in collection:
                if callback(c):
                    changes.append(c)
            return changes
                
        genericchar = pyrt.Session.Guest.GuestInstance
        
        updates = []
        
        if personality:
            p = genericchar.ExtractPersonality()
            updates += traverse_genes(p.Values)
        if appearance:
            p = genericchar.ExtractAppearance()
            updates += traverse_genes(p.Values)       
        
        if updates:
            genericchar.UpdateAll(tuple(updates))
            pyrt.Session.Guest.SynchronizeCharacterWithInstance()
            
        return len(updates)
    
    def set_genes_by_masks(self,value,*masks,appearance=False,personality=False):
        def updater(gene):
            if any((mask in gene.Id[0]) for mask in masks):
                gene.Value=value
                return True
            return False
            
        return self.modify_genes(updater,appearance=appearance,personality=personality)
    
    def set_consent_requirement(self,value):
        return self.set_genes_by_masks(0,'ConcentRequerido_',personality=True)
        
    def builtin_ai_enable_reactions(self,value):
        ai_reactions = pyrt.find_go_by_name(pyrt.Session.Guest.Impl.gameObject,'Barking Reactores')
        if not ai_reactions:
            print('no ai reactions GO found')
        else:        
            if not value:
                ai_reactions.SetActive(False)
                print('builtin ai reactions disabled')
            else:
                ai_reactions.SetActive(True)
                print('builtin ai reactions enabled')
            
        sml = find_gameobject('ScenaManagersLogic',root=pyrt.Session.Guest.Impl.gameObject)
        if not sml:
            print('ScenaManagersLogic not found')
        else:
            sml.SetActive(False)

    def builtin_ai_clear_genetics(self):
        return self.set_genes_by_masks(0, 'EstimulacionGenerada', 'EstGenPorGruEstimulado', personality=True)
        
    def ai_set_root_behavior(self,behavior):
        assert behavior is None or isinstance(behavior,Behavior)
        if behavior:
            _pyrt.ai.SetBehaviorRoot(behavior._bt_node)
        else:
            _pyrt.ai.SetBehaviorRoot(None)
            
    def set_genes_from_package(self,filename):
        pyrt.gio_apply_genes_from_package(filename)
        
    def terminate_interview(self):
        pyrt.Session.TerminateInterview()
        
    

class Player:
    
    def __init__(self):
        self.animator_node = pyrt.find_go_by_name(pyrt.Session.Player.GameObject,'CC_Base_Body').transform.parent

    def say(self,text):
        return pyrt.dialogue('You',text)
        
    def menu(self,texts):
        if isinstance(texts,str):
            return pyrt.dialogue_response([texts])
            
        elif isinstance(texts,list) or isinstance(texts,tuple):
            handlers = dict()
            params = []
            for p in texts:
                if p is None:
                    pass
                elif isinstance(p,str):
                    params.append(p)
                elif isinstance(p,tuple):
                    if len(p)==3:
                        (label,handler,cond) = p
                    else:
                        (label,handler) = p
                        cond = True
                    if cond:
                        handlers[len(params)]=handler
                        params.append(label)
                else:
                    print('TODO: report unexpected menu entry object '+p)
                    pass
                    
            if len(handlers)==0:
                return pyrt.dialogue_response(params)
            else:
                return self._gen_menu(params,handlers)
        else:
            return pyrt.dialogue_response(texts)
            
    def _gen_menu(self,params,handlers):
        yield pyrt.dialogue_response(params)
        handler = handlers.get(self.last_response)
        if handler is not None:
            yield handler()
        
        
    @property
    def last_response(self):
        return pyrt.get_last_response()
        
    def rest_at(self,pos,euler):
        self.animator_node.rotation = Quaternion.Euler(euler[0],euler[1],euler[2])
        self.animator_node.position = Vector3(pos[0],pos[1],pos[2])

    def stand_up(self):
        self.animator_node.localRotation = Quaternion.identity
        self.animator_node.localPosition = Vector3.zero
        
    def is_resting(self):
        return self.animator_node.localRotation!=Quaternion.identity
    

class Narrator:
    def say(self,text):
        return pyrt.dialogue('','<i>'+text+'</i>')

class _InteractionWrapper:
    def __init__(self,interaction):
        self._impl=interaction
        
    @property
    def name(self):
        return self._impl.DisplayName
        
    def submit(self):
        return pyrt.execute_interaction(self._impl)
        
    def gen_submit(self):
        yield self.submit()
        
    def submit_later(self):
        Strand().invoke_next(self.gen_submit)
        
        
class LookAtTarget:

    def __init__(self,*args,root=None):
        go = find_gameobject(*args,root=root)
        if not go:
            raise Exception('GameObject ',args,'not found')
            
        self._transform = go.transform
        self._enabled=False
        
    @property
    def enabled(self):
        return self._enabled
        
    @enabled.setter
    def enabled(self,value):
        if self._enabled==value:
            return
            
        self._enabled = value
        
        if value:
            Strand().invoke_next(self._look_at_loop)
            
    def _look_at_loop(self):
        while self.enabled:
            pyrt.set_look_at_target(self._transform,5)
            yield wait_ms(1000)
        pyrt.set_look_at_target(None,0)
            

class Behavior(object):

    def __init__(self,*args,reactors=None,cond=None):
        
        if len(args):
            raise Exception('Behavior does not accept positional arguments')
            
        if reactors:
            reactors = self._parse_reactors(reactors)
            
        if isinstance(cond,Var):
            _cond = cond
            cond = lambda: bool(_cond)
        
            
        self._bt_node = pystory.BehaviorNode(cond,reactors)
            
    def _parse_reactors(self,reactors):
        handlers = []
        
        for b in reactors:
            c = dict(b)
            
            stimulus = c.pop('stimulus')
            receiver = c.pop('receiver',None)
            sender = c.pop('sender',None)
            handler = c.pop('reactor')
            
            if stimulus:
                stimulus = self._parse_enum_tuple(stimulus,pystory.StimulusType)
                
            if receiver:
                receiver = self._parse_enum_tuple(receiver,pystory.HumanBodyPartsEng)
                
            if sender:
                sender = self._parse_enum_tuple(sender,pystory.SenderBodyPartEng)
            
            if c.keys():
                print('Unexpected behavior params '+c.keys())
                
            handlers.append(pystory.StimuliReactor(stimulus,receiver,sender,handler))
            
        return tuple(handlers)
        
    def add(self,*args,**kwargs):
        if len(args)>0:
            for bh in args:
                assert isinstance(bh,Behavior)
                self._bt_node.Add(bh._bt_node)
        else:
            bh = Behavior(**kwargs)
            self.add(bh)
            
    def _parse_enum_tuple(self,value,enum_type):
        if isinstance(value,str):
            return ( System.Enum.Parse(enum_type,value), )
        else:
            tmp = []
            for v in value:
                tmp.append( System.Enum.Parse(enum_type,v) )
            return tuple(tmp)
            
        
    
class Var:
    def __init__(self,name,initial):
        self.name = name
        self._value=initial
        
    @property
    def value(self):
        return self._value
        
    def set(self,value):
        self._value = value
        print('set {} = {}'.format(self.name,self._value))
        
    def add(self,value):
        self._value += value
        print('set {} = {} [dv {}]'.format(self.name,self._value,value))
        
    def __bool__(self):
        return bool(self._value)
        
class InteractiveObject:

    def __init__(self,*args,root=None):
        go = find_gameobject(*args,root=root)
        self._io = pyrt.make_interactive(go)
        
    @property
    def label(self):
        return self._io.label
        
    @label.setter
    def label(self,value):
        self._io.label=value
        
    @property
    def handler(self):
        return self._io.handler
        
    @handler.setter
    def handler(self,value):
        self._io.handler=value

    @property
    def transform(self):
        return self._io.transform