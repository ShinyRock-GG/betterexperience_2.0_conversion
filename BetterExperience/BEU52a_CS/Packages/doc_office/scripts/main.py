import math
import pycs

from scene import G,P,N,door_interaction,chart_panel,StoryStep,MenuLoop,G_strand,setup_scene

import brain

intro = True
appointment = False
chair_stage = pycs.Var('chair_stage',False)
bed_stage = pycs.Var('bed_stage',None)
exam_explained = pycs.Var('exam_explained',False)
task_progress = pycs.Var('task_progress',0)
going_away = pycs.Var('going_away',False)

story_step = None

unlock_hands = [brain.mem_checking_hands]
unlock_arms = [brain.mem_checking_hands,brain.mem_checking_arms]
unlock_neck = [brain.mem_checking_hands,brain.mem_checking_arms,brain.mem_checking_neck]
unlock_mouth = unlock_neck + [brain.mem_checking_mouth]
unlock_back = unlock_mouth + [brain.mem_checking_back]
unlock_breasts = unlock_back + [brain.mem_checking_breasts]
unlock_legs = unlock_breasts + [brain.mem_checking_legs]
unlock_belly = unlock_legs + [brain.mem_checking_belly]
unlock_vag = unlock_belly + [brain.mem_checking_vag]
unlock_finale = unlock_vag + [brain.mem_exam_complete]
unlock_grand_finale = unlock_finale + [brain.mem_grand_finale]

bed_story_steps = dict(
    hands=StoryStep(poi='Bed',pose='CheckArms',task=['hands'],  then='arms', unlocks=unlock_hands),
    arms= StoryStep(poi='Bed',pose='CheckArms',task=['arms','forearms','sholders'],then='pulse',unlocks=unlock_arms),
    pulse=StoryStep(poi='Bed',pose='CheckArms',task=['hands'],tool=['finger'],count=1,min_duration=5,then='post_pulse',unlocks=unlock_arms),
    post_pulse=StoryStep(poi='Bed',pose='CheckArms',task=[],tool=[],then='read_chart',unlocks=unlock_arms), # skip neck and head stage
    neck= StoryStep(poi='Bed',pose='Binding',task=['neck','jaw'], then='head',unlocks=unlock_neck),
    head= StoryStep(poi='Bed',pose='Binding',task=['head'],then='read_chart',unlocks=unlock_neck),
    read_chart=StoryStep(poi='Bed',pose='ReadChart',task=[],then='face',unlocks=unlock_neck),
    face= StoryStep(poi='Bed',pose='Binding',task=['jaw','lips','nose','cheeks','face','eyebrows','eyes'], then='mouth',unlocks=unlock_neck),
    mouth=StoryStep(poi='Bed',pose='Binding',task=['innermouth'],tool=['finger'],event='penetration', then='standup', unlocks=unlock_mouth),
    
    standup =   StoryStep(poi='MiddleRoom',pose=None,task=[],then='back',unlocks=unlock_mouth),
    back =      StoryStep(poi='MiddleRoom',pose=None,task=['back'],then='touch_toes',unlocks=unlock_back),
    touch_toes = StoryStep(poi='MiddleRoom',pose='TouchToes',task=[],then='breasts',unlocks=unlock_back),
    breasts =    StoryStep(poi='MiddleRoom',pose=None,task=['breasts','nipples'],then='legs',unlocks=unlock_breasts),
    legs = StoryStep(poi='Bed',pose='Spread_LDBD',task=['legs','thighs','feet'],then='belly',unlocks=unlock_belly),
    belly = StoryStep(poi='Bed',pose='Spread_LDBD',task=['belly','stomach'],then='vag',unlocks=unlock_belly),
    vag   = StoryStep(poi='Bed',pose='Spread_LDBD',task=['vag'],tool=['finger'],event='penetration',then='finale',unlocks=unlock_vag),
    
    finale = StoryStep(poi='Bed',pose='Spread_LDBD',task=['anus'],tool=['finger'],event='penetration',count=1,then='grand_finale',unlocks=unlock_finale),
    grand_finale = StoryStep(poi=None,pose=None,task=[],then=None,unlocks=unlock_grand_finale),
)

hint_label = '<i>...what was I doing?...</i>'

def start():
    setup_scene()
    
    G.interaction_handler = pycs.main_strand.callable_immediate(talk_to_g)
    
    brain.on_touch.listener = touch_event_listener
    brain.on_max_negative_emotion.handler=on_max_negative_emotion
    
    yield G.teleport('Original_GoTo')
    
    yield N.say('Welcome to interactive showcase\n'
                'The goal of this demo is to test touching based interaction scripting'
               )
    yield N.say('This script is based on  Eskarn\'s Doctor Story for XStoryPlayer')
        
    yield P.say('And thats the last patient gone. Time to go home')
    
    #yield bed_stage_shortcut('back')
    
def talk_to_g():

    if G.busy or G_strand.busy:
        yield N.say('{} is busy now.'.format(G.name))
        yield pycs.stop_dialogue()
        return

    global intro
    if intro:
        intro=False
        yield N.say('Huh is there one more patient?')
        yield P.menu([
            ('Hello?',intro_dlg),
            ('Huh?', intro_dlg),
            ('What the fuck are you doing there?',intro_rude)
        ])
    elif chair_stage:
        yield P.say('So, {}...'.format(G.first_name))
        yield P.menu([
            ('How can i help you today {}'.format(G.first_name),chair_interview),
            ('The fuck you want?',conclusion_rude),
            ('Nevermind.',None)
        ])
    else:
        yield talk_examination()
        
    yield pycs.stop_dialogue()

def intro_rude():
    yield G.say('...I am {} and I need an examination'.format(G.name))
    yield P.say('And I don\'t give a fuck about your needs')
    yield conclusion_rude()

def conclusion_rude():
    yield G.say('Ah why are you being so hostile')
    yield P.say('Because some woman wants a damn 45 minute examination after working hours')
    yield G.say('You could have told me to come back tomorrow')
    if appointment:
        yield P.say('And I did that. And what you said? \"It\'s urgent!\"')
    yield P.say('No no it\'s fine, by the end of this examination you will be naked with my fingers up your cunt')
    yield P.say('Feeling your sexy ass up for the next 45 minutes will make it all worth it')
    yield finish_rude()
    
def finish_rude():
    yield G.say('Yea that\'s not fucken happening, I\'m leaving now')
    escape_sequence()

@G_strand.callable_later
def escape_sequence():
    
    yield G.go_to_poi('GoAway')
    G.terminate_interview()
    yield pycs.wait_ms(1000)
    
def intro_dlg():
    yield G.say('Hello, my name is {}'.format(G.name))
    yield G.say('I urgently need examination')
    
    yield P.menu([
        ( '{}, take a seat in my office'.format(G.first_name), finish_intro),
        ( 'Sorry {}, you will have to make an appointment'.format(G.first_name), intro_dlg2 ),
        ( 'I don\'t give afuck about your problems, girl. I\'m goin\' home', conclusion_rude)
    ])
    
def intro_dlg2():
    global appointment
    appointment=True
    yield G.say('But doctor, I really need it today. Tomorrow will be too late.')
    yield P.menu([
        ('Alright, take a seat in my office.',finish_intro),
        ('And I really need to have sex after work, but I don\'t have a secretary! You can give me a blowjob now, and I will examinate you.',finish_rude)
    ])

def finish_intro():
    yield G.say('Thank you, doctor')
    yield chair_0()
    
@G_strand.callable_next
def chair_0():
    yield G.go_to_poi('Chair')
    G.comment('You have a very strange chair, doctor')
    yield G.apply_posture('ClimbLow.Chair.Chair')
    chair_stage.set(True)

def chair_0_shortcut():
    yield G.teleport('Chair')
    yield G.apply_posture('ClimbLow.Chair.Chair')
    global intro
    intro = False
    chair_stage.set(True)
    
def chair_interview():
    yield P.say('{}, what is your emergency?'.format(G.first_name))
    if not door_interaction.closed:
        yield G.say('Doctor, could you please close the door')
        yield P.say('Yeah, sure.')
        return
        
    yield G.say('I just need an examination for my new job I\'m starting tomorrow')
    yield G.say('They did not tell me I needed it until an hour ago')
    yield P.say('What kind of job are you doing?')
    yield G.say('It\'s just a waitress job')
    yield P.say('I hope you do well')
    yield G.say('Thanks')
    yield P.say('Ok, to start, can you sit on the bed?')
    yield G.say('Sure')
    
    bed_stage_init()
    
@G_strand.callable_next
def bed_stage_init():
    yield G.apply_posture('Stand')
    yield G.go_to_poi('Bed')
    yield G.comment('You have a very strange furniture in your office')
    yield G.apply_posture('ClimbHigh.Bed.Bed')
    
    chair_stage.set(False)
    start_bed_stage('hands')
    
def bed_stage_shortcut(stage):
    poi = bed_story_steps[stage].poi
    
    yield G.teleport(poi)
    if poi=='Bed':
        yield G.apply_posture('ClimbHigh.Bed.Bed')
    else:
        pass
            
    global intro
    intro = False
    chair_stage.set(False)
    start_bed_stage(stage)
    
def talk_examination():
    yield P.say('{}...'.format(G.first_name))
    if bed_stage.value =='hands':
        
        def hint():
            yield N.say('I need to check her hands by touching them')
            
        def explanation():
            yield P.say('I\'m going to start by checking your hands')
            yield G.say('Okay')
            yield stage_explained()
        
        yield P.menu([
            ('I\'m going to start by checking your hands',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
    
    elif bed_stage.value == 'arms':
        
        def hint():
            yield N.say('I need to check her arms')
            
        def explanation():
            yield P.say('Next i need to check your arms')
            yield G.say('Okay')
            yield stage_explained()
        
        yield P.menu([
            ('Next i need to check your arms',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
    
    elif bed_stage.value == 'pulse':
        
        def hint():
            yield N.say('I need to check her pulse using finger')
            
        def explanation():
            yield P.say('Next i need to check your pulse with my finger')
            yield G.say('Okay')
            yield stage_explained()
        
        yield P.menu([
            ('Next i need to check your pulse',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
    
    elif bed_stage.value == 'post_pulse':
    
        

        def fast_pulse():
            yield P.say('Your pulse is a little bit fast, are you feeling ok?')
            yield G.say('Yes i feel fine')
            step_forward()
            
        def aroused_pulse():
            yield P.say('Your pulse is fast, are you getting sexual aroused by me touching you?')
            yield G.say('I ahhh... ummm... a little?')
            yield P.say('I should have worded that better, but don\'t be embarrassed, thats perfectly normal')
            # TODO: aaand??
            step_forward()
        
        yield P.menu([
            ('Your pulse is a little bit fast, are you feeling ok?', fast_pulse, G.stats.pleasure.value>50),
            ('Your pulse is fast, are you getting sexual aroused by me touching you?', aroused_pulse, G.stats.pleasure.value>50),
            ('Your pulse is normal', step_forward, G.stats.pleasure.value<=50),
            ('Nevermind',None)
        ])
        
        if bed_stage.value != 'post_pulse':
            yield talk_examination()

    elif bed_stage.value == 'neck':
        
        def hint():
            yield N.say('I need to touch her neck with my hand')
            
        def explanation():
            yield P.say('Next I need to check your neck')
            yield G.say('Okay')
            yield stage_explained()
        
        yield P.menu([
            ('Next I need to check your neck',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'head':
        
        def hint():
            yield N.say('I need to touch her head with my hand')
            
        def explanation():
            yield P.say('Next I need to check your head')
            yield G.say('Okay')
            yield stage_explained()
        
        yield P.menu([
            ('Next i need to check your head',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
    
    elif bed_stage.value == 'read_chart':
        
        def explanation():
            yield P.say('Head looks fine, can i get you to read the lowest line you can on the eye chart')
            yield G.say('Okay')
            yield stage_explained()
            
            yield pycs.stop_dialogue() # wait until user exit conversation
            
            yield read_chart_script()
        
        yield P.menu([
            ('Head looks fine',explanation,not exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'face':
        
        # TODO: closed eyes gesture
        
        def hint():
            yield N.say('I need to touch her face with my hand')
            
        def explanation():
            yield P.say('Very good, Now it\'s onto testing some nerve functions')
            yield G.say('Okay')
            yield P.say('Just close your eyes and tell me if you can feel my fingers on your face')
            yield stage_explained()
        
        yield P.menu([
            ('Very good',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'mouth':
        
        # TODO: closed eyes gesture
        
        def hint():
            yield N.say('I need to stick my finger into her mouth')
            
        def explanation():
            yield P.say('Now i need to check your mouth')
            yield G.say('Okay')
            yield P.say('But I need to inform you that I cannot hold any tool in my hands\n'
            'That\'s why I will use my finger as a substitute.\n'
            'Are you fine with this?')
            yield G.say('...Okay, I think...')
            
            yield stage_explained()
            
            
        
        yield P.menu([
            ('Now I need to check your mouth',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'standup':
    
        
        def explanation():
            yield P.say('Mouth looks good, can i get you to stand in the middle of the room')
            yield G.say('Okay')
            yield pycs.stop_dialogue()
            
            yield stage_explained()
        
        yield P.menu([
            ('Your mouth looks good',explanation),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'back':
        
        def hint():
            yield N.say('I need to touch her back with my hand')
            
        def explanation():
            yield P.say('You need to take your dress off, I need to check your spine')
            yield G.say('Okay')
            
            yield stage_explained()
            
            
            
        
        yield P.menu([
            ('Now I need to check your spine',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'touch_toes':
        
        def hint():
            yield P.say('{}, is this the best you can do?'.format(G.first_name))
            yield G.say('Doctor, I\'m so sorry.')
            yield P.say('No need to apologize. It\'s not your fault.')
            yield P.say('Anyway, it\'s not that bad.')
            yield G.say('Oh, good.')
            
            yield G.play_clip(None)
            
            step_forward()
            
        def explanation():
            yield P.say('Can you touch your toes while keeping your legs straight?')
            yield G.say('Okay')
            
            yield stage_explained()
            
            
            
        
        yield P.menu([
            ('Can you touch your toes?',explanation,not exam_explained),
            ('{}...'.format(G.first_name),hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'breasts':
        
        def hint():
            yield N.say('I need to touch her breasts with my hand')
            
        def explanation():
            yield P.say('I need to check your breasts now, so I need you to take your bra off')
            if G.stats.pleasure.value > 50:
                yield G.say('I won\'t mind if you play with them a little')
            else:
                yield G.say('Okay')
            
            yield stage_explained()
            
            
            
        
        yield P.menu([
            ('I need to check your breasts now',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'legs':
        
        def hint():
            yield N.say('I need to touch her legs with my hand')
            
        def explanation():
            yield P.say('Can i get you to lay on the table')
            yield P.say('I need to check your legs now')
            yield G.say('Okay')
            
            yield pycs.stop_dialogue()
            
            yield stage_explained()
            
            
            
        
        yield P.menu([
            ('Can i get you to lay on the table',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'belly':
        
        def hint():
            yield N.say('I need to touch her belly with my hand')
            
        def explanation():
            yield P.say('I need to check your stomach now')
            yield G.say('Okay')
            
            yield pycs.stop_dialogue()
            
            yield stage_explained()
            
            
            
        
        yield P.menu([
            ('I need to check your stomach now',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'vag':
        
        def hint():
            yield N.say('I need to poke her vagina with my finger')
            
        def explanation():
            yield P.say('I need to check your vagina now. Can you take off your panties?')
            yield G.say('Okay')
            
            yield pycs.stop_dialogue()
            
            
            pycs.invoke_later(take_off_panies_seq)
                
            yield stage_explained()
            
            
            
            
        
        yield P.menu([
            ('I need to check your vagina',explanation,not exam_explained),
            (hint_label,hint,exam_explained),
            ('Nevermind',None)
        ])
        
    elif bed_stage.value == 'finale':
        

        def explanation():
            yield P.say('The tests are done. Now I need to fill your papers.')
            yield G.say('Are you sure that\'s what you need to do now?')
            yield P.say('What do you mean?')
            yield G.say('I am naked and horny. How about quickie?')
            yield P.say('Sure')
                        
            yield stage_explained()
            
            
            
        if exam_explained:
            yield sandbox_menu()
        else:
            yield P.menu([
                ('And we\'re done',explanation,not exam_explained),
                ('Nevermind',None)
            ])
        
    elif bed_stage.value == 'grand_finale': 
        yield sandbox_menu()

def sandbox_menu():

    loop = MenuLoop()
    
    def grand_explanation():
        yield P.say('How about anal?')
        yield G.say('I don\'t know...')
        yield P.say('Why not?')
        yield G.say('I never tried that')
        yield P.say('This is a good opportunity to try it under doctor\'s observation')
        yield G.say('I\'m still not sure')
        yield P.say('I\'ll be gentle, I promise')
        yield G.say('Okay, let\'s try')
        
                    
        yield stage_explained()
    
    def poses():
        l = []
        
        for action in G.enumerate_transitions():
            l.append( (action.name, action.submit_later ) )
        
        l.append( ('Nevermind',loop) )
        
        return P.menu(l)
        
    def gotos():
        l = []
        
        for action in G.enumerate_gotos():
            l.append( (action.name, action.submit_later ) )
        
        l.append( ('Nevermind',loop) )
        
        return P.menu(l)
    
    while loop.active:
        loop.reset()
        
        yield P.menu([
            ( 'How about anal?', grand_explanation, bed_stage.value == 'grand_finale' and not exam_explained),
            ( 'Can you...', poses ),
            ( 'Let\'s move to...', gotos ),
            ( 'Nevermind', None )
        ])
        

def take_off_panies_seq():
    # stand up
    yield G.apply_posture('Stand')

    # trigger undress
    yield G.take_off_slots_sync(['vagina','legs','butt'])

def start_bed_stage(stage):
    global story_step
    bed_stage.set(stage)
    exam_explained.set(False)
    story_step=bed_story_steps[stage]
    
@G_strand.callable_next
def stage_explained():

    exam_explained.set(True)
    task_progress.set(story_step.count)

    if story_step.unlocks:
        for u in story_step.unlocks:
            u.set(True)
      
    chart_panel.enabled = bed_stage.value=='read_chart'
    
    if brain.mem_checking_mouth:
        G.set_consent_requirement(0)
        
    if brain.mem_checking_back:
        yield G.take_off_slots_sync(['chest','legs'])
    if brain.mem_checking_breasts:
        yield G.take_off_slots_sync(['nipples'])
    if brain.mem_checking_vag:
        yield G.take_off_slots_sync(['vagina','legs','butt'])
        
    if story_step.poi:
        if G.poi != story_step.poi:
            yield G.apply_posture('Stand')
            yield G.go_to_poi(story_step.poi)
            
        if story_step.poi=='Bed' and 'ClimbHigh' not in G.posture:
            yield G.apply_posture('ClimbHigh.Bed.Bed')

        yield G.play_clip(story_step.pose)
    
    if bed_stage.value=='face':
        pycs.invoke_async(eye_closing_sequence)
    elif bed_stage.value=='standup':
        step_forward()

def eye_closing_sequence():
    while bed_stage.value=='face':
        G.set_eyes_expression('close',5)
        yield pycs.wait_ms(1000)
    

def update_task_progress(evt):
    expected = story_step.min_duration*story_step.count
    already_completed = story_step.progress < expected
    completed=False
    pre_step_progress = math.ceil(story_step.progress)
    story_step.progress += evt.delta_time
    if math.ceil(story_step.progress)>pre_step_progress:
        pycs.overlay_notify('Task progress {}%'.format(math.ceil(story_step.progress*100/expected)),2)
    
    completed = story_step.progress >= expected

    if completed and not already_completed:
        step_forward()
        
        if completed:
            pycs.invoke_later(notify_completion)

def step_forward():
    next_stage = bed_story_steps[bed_stage.value].then
    start_bed_stage(next_stage)

def notify_completion():
    if bed_stage.value == 'grand_finale':
        yield N.say('I need to ask her about anal')
    else:
        pycs.overlay_notify('This task is complete')

def touch_event_listener(evt):
    if exam_explained:
    
        if evt.stimulus==story_step.event and evt.receiver in story_step.task and evt.sender in story_step.tool:
            update_task_progress(evt)
        else:
            pass
            
    else:
        pass

def on_max_negative_emotion():
    if going_away:
        return
    going_away.set(True)
    
    def monologue():
        yield G.say('I\'m leaving!')
        escape_sequence()
        
    G_strand.invoke_later(monologue)

@G_strand.callable_next
def read_chart_script():
    delay = 1000
    
    
    
    yield G.comment('L')
    yield pycs.wait_ms(delay)
    yield G.comment('E')
    yield pycs.wait_ms(delay)
    yield G.comment('F')
    yield pycs.wait_ms(delay)
    yield G.comment('O')
    yield pycs.wait_ms(delay)
    yield G.comment('D')
    yield pycs.wait_ms(delay)
    yield G.comment('P')
    yield pycs.wait_ms(delay)
    yield G.comment('C')
    yield pycs.wait_ms(delay)
    yield G.comment('T')
    yield pycs.wait_ms(delay)
    
    chart_panel.enabled=False
    
    yield G.play_clip('Idle')
    
    chart_panel.enabled=False
    
    step_forward()