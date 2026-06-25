import pycs
from scene import G

hands_parts = ['hands','finger']
vag_parts = ['perineum','vag','labia']

mem_checking_hands = pycs.Var('mem_checking_hands',False)
mem_checking_arms = pycs.Var('mem_checking_arms',False)
mem_checking_neck = pycs.Var('mem_checking_neck',False)
mem_checking_mouth = pycs.Var('mem_checking_mouth',False)
mem_checking_back = pycs.Var('mem_checking_back',False)
mem_checking_breasts = pycs.Var('mem_checking_breasts',False)
mem_checking_legs  = pycs.Var('mem_checking_legs',False)
mem_checking_belly = pycs.Var('mem_checking_belly',False)
mem_checking_vag = pycs.Var('mem_checking_vag',False)
mem_exam_complete = pycs.Var('mem_exam_complete',False)
mem_grand_finale = pycs.Var('mem_grand_finale',False)

mem_cock_out = pycs.Var('mem_cock_out',0)

default_pleasure_gain = 0.75
breasts_pleasure_gain = default_pleasure_gain * 1.5
oral_pleasure_gain  = default_pleasure_gain * 1.5
vaginal_pleasure_gain = default_pleasure_gain * 3
groin_pleasure_gain = breasts_pleasure_gain
finger_pleasure_gain = default_pleasure_gain * 2.25

@pycs.with_cooldown(5000)
def complain_penis():
    if mem_cock_out.value==0:
        G.comment('Doctor, what\'s going on?')        
    elif mem_cock_out.value==1:
        G.comment('Is this some sort of a joke?')
    elif mem_cock_out.value==2:
        G.comment('Are you crazy, doctor?')
    elif mem_cock_out.value>2:
        idx = mem_cock_out.value % 2
        if idx==0:
            G.comment('Why the fuck do you have your cock out?')
        else:
            G.comment('What the fuck? Take your dick away!')
            
    mem_cock_out.add(1)
    
@pycs.with_cooldown(5000)
def complain_penis_touch():
    G.comment('Take you penis away you sick fuck!')

@pycs.with_cooldown(5000)    
def complain_about_touch(evt):
    
    if evt.sender=='finger':
        G.comment('Can you stop poking me?')
    else:
        G.comment('Keep your hands away from my {}!'.format(evt.receiver))

@pycs.with_cooldown(2000)
def complain_penetration():
    G.comment('Take it out you motherfucker!')

@pycs.with_cooldown(5000)
def complain_photo(evt):
    G.comment('Take away your camera!')
    G.stats.rage.add(10)

def react_to_penis(evt):
    G.stats.rage.add((1+mem_cock_out.value)*evt.delta_time)
    
    complain_penis()

def react_to_penis_touch(evt):
    G.stats.rage.add(20*evt.delta_time)
    
    complain_penis_touch()
    
def negative_penetration_reactor(mul=10):
    
    def impl(evt):
        print (evt.receiver)
        complain_penetration()
        G.stats.rage.add(mul*evt.delta_time)
        return True
    
    return impl
    

def react_to_hand_touch(evt):
    G.stats.rage.add(1*evt.delta_time)
    complain_about_touch(evt)
    
def positive_reactor(mul=0,paingain=-2):
    
    def impl(evt):
        value = mul*evt.delta_time
        G.stats.pleasure.add(value)
        G.stats.pain.add(paingain*evt.delta_time)
        return True
        
    return impl

def negative_reactor(mul=1,dispatcher=None):
    def impl(evt):
        if dispatcher:
            dispatcher(evt)
            
        value = mul*evt.delta_time
        G.stats.rage.add(value)
        return True
    return impl

root_behavior = pycs.Behavior(reactors=[
    dict(stimulus='touch', sender='penis', reactor=react_to_penis_touch),
    dict(stimulus='touch', sender=['hands','finger'], reactor=react_to_hand_touch),
    dict(stimulus='penetration', reactor=negative_penetration_reactor()),
    dict(stimulus='touch', receiver=['buttocks','breasts','nipples'] , reactor=negative_reactor(mul=20)),
    dict(stimulus='photo', reactor=negative_reactor(mul=10,dispatcher=complain_photo)),
])

root_behavior.add( cond=lambda: not mem_exam_complete, reactors=[
    dict(stimulus='observe', receiver='penis', reactor=react_to_penis)
])

def on_touch(evt):
    if on_touch.listener:
        on_touch.listener(evt)
    return False
    
on_touch.listener=None

root_behavior.add(reactors=[
    dict(stimulus=['touch','penetration'], sender=['hands','finger'], reactor=on_touch)
])

root_behavior.add( cond=mem_checking_hands, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver='hands', reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_arms, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['arms','forearms','shoulders'], reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_neck, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['chest','neck','jaw'], reactor=positive_reactor(mul=default_pleasure_gain)),
])

root_behavior.add( cond=mem_checking_neck, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['head','lips','nose','cheeks','face','hair','eyebrows','innermouth'], reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_mouth, reactors=[
    dict(stimulus='penetration', sender=['finger'], receiver=['innermouth'], reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_back, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['back'], reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_breasts, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['breasts','nipples'], reactor=positive_reactor(mul=breasts_pleasure_gain))
])

root_behavior.add( cond=mem_checking_legs, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['legs','hips','thighs','feet'], reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_checking_belly, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=['belly','stomach','belly_button','waist'], reactor=positive_reactor(mul=default_pleasure_gain)),
    dict(stimulus='touch', sender=hands_parts, receiver=['groin'], reactor=positive_reactor(mul=groin_pleasure_gain))
])

root_behavior.add( cond=mem_checking_vag, reactors=[
    dict(stimulus='touch', sender=hands_parts, receiver=vag_parts + ['buttocks'], reactor=positive_reactor(mul=breasts_pleasure_gain)),
    dict(stimulus='penetration', sender=['finger'], receiver=['vag'],reactor=positive_reactor(mul=default_pleasure_gain))
])

root_behavior.add( cond=mem_exam_complete, reactors=[
    dict(stimulus='touch', sender=['penis'],reactor=positive_reactor(mul=default_pleasure_gain)),
    dict(stimulus='touch', sender=['finger'],reactor=positive_reactor(mul=default_pleasure_gain)),
    dict(stimulus='penetration', sender=hands_parts+['penis'], receiver=['innermouth'], reactor=positive_reactor(mul=oral_pleasure_gain)), # sucking
    dict(stimulus='penetration', sender=hands_parts, receiver=['vag'], reactor=positive_reactor(mul=finger_pleasure_gain)),                # fingering
    dict(stimulus='penetration', sender=['penis'], receiver=['vag'],reactor=positive_reactor(mul=vaginal_pleasure_gain)),                  # vaginal
    dict(stimulus='penetration', sender=['finger'], receiver=['anus'],reactor=positive_reactor(mul=0.75*finger_pleasure_gain,paingain=1)), # anal-fingering
])

root_behavior.add( cond=mem_grand_finale, reactors=[
    dict(stimulus='penetration', sender=['penis'], receiver=['anus'],reactor=positive_reactor(mul=0.75*default_pleasure_gain,paingain=2)), # anal
])


def on_max_negative_emotion():
    h = on_max_negative_emotion.handler
    if h is not None:
        h()
        
on_max_negative_emotion.handler=None

G.ai_set_root_behavior(root_behavior)
G.stats.rage.reset()
G.stats.pleasure.reset()
G.stats.pain.reset()
G.stats.consent.reset()

G.stats.rage.on_max_value = on_max_negative_emotion