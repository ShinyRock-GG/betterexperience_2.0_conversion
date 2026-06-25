using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.AI;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.PyStory.AI;

public class SimpleAi : SessionService
{
	public class SimpleEmotion
	{
		private Emocion impl;

		private float initialValue;

		public Action on_max_value { get; set; }

		public float value => impl.valorNoLimitado;

		public SimpleEmotion(Emocion impl)
		{
			this.impl = impl;
			impl.onMaxValue += Impl_onMaxValue;
			initialValue = impl.valorNoLimitado;
		}

		private void Impl_onMaxValue(Emocion obj)
		{
			if (on_max_value != null)
			{
				try
				{
					on_max_value();
				}
				catch (Exception ex)
				{
					Logger.Global.Error(ex, "on_max_value failure");
				}
			}
		}

		public void add(float value)
		{
			impl.ChangeValueNextUpdate(value);
		}

		public void reset()
		{
			if (impl.minValue > 0f)
			{
				impl.SetMinValue(0f);
			}
			impl.SetValueNextUpdate(0f);
		}
	}

	public class SimpleStimulus
	{
		private static Array ALL_FLAGS = Enum.GetValues(typeof(StimulusTrackingService.StimulusFlags));

		public StimulusTrackingService.StimulusKey Key { get; }

		public float delta_time { get; }

		public float total_duration { get; }

		public float duration { get; }

		public float intensity { get; }

		public bool covered { get; }

		public List<string> flags { get; } = new List<string>();

		public string stimulus => Key.Stimulus.ToString();

		public string receiver => Key.Receiver.ToString();

		public string sender => Key.Sender.ToString();

		public string emotion { get; private set; }

		public SimpleStimulus(StimulusTrackingService.StimulusKey key, float duration, float dv, float total_duration, float intensity, StimulusTrackingService.StimulusFlags flags, string emotion)
		{
			Key = key;
			this.duration = duration;
			delta_time = dv;
			this.total_duration = total_duration;
			this.intensity = intensity;
			this.emotion = emotion;
			foreach (object flag in ALL_FLAGS)
			{
				StimulusTrackingService.StimulusFlags f = (StimulusTrackingService.StimulusFlags)flag;
				if (f != StimulusTrackingService.StimulusFlags.none && (f & flags) != StimulusTrackingService.StimulusFlags.none)
				{
					this.flags.Add(f.ToString());
				}
			}
		}
	}

	private StimulusTrackingService trackerService;

	private List<IList<StimuliReactor>> reactorStack = new List<IList<StimuliReactor>>();

	private BehaviorNode rootBehavior;

	public SimpleEmotion Pleasure { get; private set; }

	public SimpleEmotion Anger { get; private set; }

	public SimpleEmotion Pain { get; private set; }

	public SimpleEmotion Consent { get; private set; }

	public Deseos Deseos { get; private set; }

	public void Reset()
	{
		reactorStack.Clear();
	}

	public void PushBehavior(IList<StimuliReactor> reactors)
	{
		reactorStack.Insert(0, reactors);
	}

	public void SetBehaviorRoot(BehaviorNode behaviorNode)
	{
		rootBehavior = behaviorNode;
	}

	public override void OnStart()
	{
		base.OnStart();
		trackerService = Lookup<StimulusTrackingService>();
		trackerService.AfterStimulusUpdate.Add(ProcessReactions, base.Scope);
		base.Session.OnGuestReady += delegate
		{
			InitComponents();
		};
	}

	private void InitComponents()
	{
		EmocionesFemeninas emotions = ((Component)(object)base.Session.Guest.Impl).GetComponentInChildren<EmocionesFemeninas>();
		Pleasure = new SimpleEmotion(emotions.placer);
		Anger = new SimpleEmotion(emotions.rage);
		Pain = new SimpleEmotion(emotions.dolor);
		Consent = new SimpleEmotion(emotions.consentToHero);
		Deseos = ((Component)(object)base.Session.Guest.Impl).GetComponentInChildren<Deseos>();
	}

	private void ProcessReactions()
	{
		List<SimpleStimulus> penetrations = new List<SimpleStimulus>();
		List<SimpleStimulus> touches = new List<SimpleStimulus>();
		List<SimpleStimulus> glances = new List<SimpleStimulus>();
		List<SimpleStimulus> observations = new List<SimpleStimulus>();
		List<SimpleStimulus> photos = new List<SimpleStimulus>();
		List<SimpleStimulus> exposings = new List<SimpleStimulus>();
		foreach (StimulusTrackingService.StimulusTracker s in trackerService.ActiveStimuli)
		{
			SimpleStimulus stimuli = new SimpleStimulus(s.Key, s.Duration, s.ImmediateDuration, s.HistoricalDuration, s.ImmediateIntensity, s.Flags, s.LastEmotion);
			if (s.Key.Stimulus == StimulusType.touch)
			{
				touches.Add(stimuli);
				continue;
			}
			if (s.Key.Stimulus == StimulusType.gaze)
			{
				glances.Add(stimuli);
				continue;
			}
			if (s.Key.Stimulus == StimulusType.observe)
			{
				observations.Add(stimuli);
				continue;
			}
			if (s.Key.Stimulus == StimulusType.penetration)
			{
				penetrations.Add(stimuli);
				continue;
			}
			if (s.Key.Stimulus == StimulusType.photo)
			{
				photos.Add(stimuli);
				continue;
			}
			if (s.Key.Stimulus == StimulusType.expose)
			{
				exposings.Add(stimuli);
				continue;
			}
			logger.Error("Unsupported stimulus type {0}", s.Key.Stimulus);
		}
		if (photos.Count > 0)
		{
			logger.Error("Photos {0}", photos.Count);
		}
		DispatchStimuli(penetrations, touches, photos, observations, glances);
		DispatchList(exposings);
	}

	private void DispatchStimuli(params List<SimpleStimulus>[] collections)
	{
		foreach (List<SimpleStimulus> list in collections)
		{
			if (DispatchList(list))
			{
				break;
			}
		}
	}

	private bool DispatchList(List<SimpleStimulus> list)
	{
		foreach (SimpleStimulus stimuli in list)
		{
			if (Dispatch(stimuli))
			{
				return true;
			}
		}
		return false;
	}

	private bool Dispatch(SimpleStimulus stimuli)
	{
		if (rootBehavior != null)
		{
			return rootBehavior.React(stimuli);
		}
		foreach (IList<StimuliReactor> x in reactorStack)
		{
			foreach (StimuliReactor matcher in x)
			{
				if ((matcher.Receiver != null && !matcher.Receiver.Contains(stimuli.Key.Receiver)) || (matcher.Sender != null && !matcher.Sender.Contains(stimuli.Key.Sender)) || (matcher.Stimulus != null && !matcher.Stimulus.Contains(stimuli.Key.Stimulus)))
				{
					continue;
				}
				try
				{
					if (matcher.Reactor(stimuli))
					{
						return true;
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "OOPS! reactor thrown error");
				}
			}
		}
		return false;
	}
}
