
from .core import *

class Cooldown:
    def __init__(self,cooldown):
        self.cooldown = 0.001 * cooldown
        self.cooltime = 0
        
        
    def fire(self):
        if self.cooltime < Time.time:
            self.cooltime = Time.time + self.cooldown
            return True
        else:
            return False

def with_cooldown(cooldown):
    cd = Cooldown(cooldown)
    
    def base_wrapper(fn):
        
        #@functools.wraps(fn)
        def wrapper(*args,**kwargs):
            if cd.fire():
                fn(*args,**kwargs)
        
        return wrapper
    
    return base_wrapper

def with_invoke_async(fn):
    def wrapper():
        return invoke_async(fn)
    return wrapper

def with_invoke_later(fn):
    def wrapper():
        return invoke_later(fn)
    return wrapper
    
def with_invoke_next(fn):
    def wrapper():
        return invoke_next(fn)
    return wrapper
    
    
def with_enqueue_once(fn):
    def wrapper():
        yield fn()        
        wrapper.enqueued=False
        
    def proxy():
        if wrapper.enqueued:
            return
        wrapper.enqueued=True
        return wrapper()
        
    wrapper.enqueued=False
    return proxy
    
    
def invoke_next(fn):
    return main_strand.invoke_next(fn)
        

def invoke_later(fn):
    return main_strand.invoke_later(fn)
        
def invoke_async(fn):
    return Strand().invoke_next(fn)