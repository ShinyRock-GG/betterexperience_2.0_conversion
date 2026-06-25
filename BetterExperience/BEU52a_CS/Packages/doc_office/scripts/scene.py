import pycs


G = pycs.Guest()
P = pycs.Player()
N = pycs.Narrator()

G_strand = pycs.Strand('G_strand')

def door_interaction():
    offset = 1.3
    transform = door.transform.parent
    closed = door_interaction.closed
    
    if closed:
        p = transform.position
        transform.position = pycs.Vector3(p.x-offset,p.y,p.z)
    else:
        p = transform.position
        transform.position = pycs.Vector3(p.x+offset,p.y,p.z)
    
    closed = not closed    
    door_interaction.closed = closed
    
    if closed:
        door.label='Open the door'
    else:
        door.label='Close the door'
    
door_interaction.closed=True

door = pycs.InteractiveObject('TheDoor')
door.label = 'Open the door'
door.handler = door_interaction

# detect door state after reload
door_interaction.closed = door.transform.parent.position.x>-4

chart_panel = pycs.LookAtTarget('Panel')

class StoryStep:

    def __init__(self,poi,pose,task,then,unlocks=None,tool=['hands'],count=5,min_duration=2,event='touch'):
        self.poi = poi
        self.pose= pose
        if isinstance(task,str):
            task = (task,)
        self.task = task
        self.then = then
        if not unlocks:
            unlocks = []
        self.unlocks = unlocks
        self.tool = tool
        self.count=count
        self.min_duration=min_duration
        self.event=event
        self.progress = 0
        

class MenuLoop:

    def __init__(self):
        self.active = True
        
    def __call__(self):
        self.active=True

    def __bool__(self):
        return self.active
        
    def reset(self):
        self.active=False
        
        
def setup_scene():
    # disable default dialogs on max emotion

    
    #install personality
    G.set_genes_from_package('\\misc\\personality_preset.json')
    #disable vanilla talks
    G.builtin_ai_enable_reactions(False)
    G.builtin_ai_clear_genetics()
    
    G.install_custom_interaction_handler()