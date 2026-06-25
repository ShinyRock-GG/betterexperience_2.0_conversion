using System.Collections.Generic;
using System.Linq;
using BetterExperience.GameScopes;
using BetterExperience.Utils;

namespace BetterExperience.Features.SceneCameras;

internal class PoseCameraRegistry
{
	private Logger logger = new Logger();

	private PersistenceService persistenceService;

	private SceneCameraSettingsNg settings;

	public PoseCameraRegistry(PersistenceService persistenceService)
	{
		this.persistenceService = persistenceService;
		settings = persistenceService.Persisted(() => new SceneCameraSettingsNg());
		foreach (List<CameraSettings> value in settings.PoseCameras.Values)
		{
			value.Sort((CameraSettings a, CameraSettings b) => a.Name.CompareTo(b.Name));
		}
	}

	private string GuessNextName(string pose)
	{
		List<string> list = (from x in settings.PoseCameras.GetValueOrDefault(pose, () => new List<CameraSettings>())
			select x.Name).ToList();
		for (int num = list.Count; num < 100; num++)
		{
			string text = pose + "_" + num;
			if (!list.Contains(text))
			{
				return text;
			}
		}
		return null;
	}

	public void Save(string pose, CameraSettings camera)
	{
		pose = Extensions.GetValueOrDefault(settings.PoseToPose, pose, pose);
		if (camera.Name == null)
		{
			camera.Name = GuessNextName(pose);
		}
		if (camera.Name == null)
		{
			logger.Error("Unable to guess filename for pose {0}", pose);
			return;
		}
		List<CameraSettings> valueOrAdd = settings.PoseCameras.GetValueOrAdd(pose, () => new List<CameraSettings>());
		CameraSettings cameraSettings = valueOrAdd.Find((CameraSettings cam) => cam.Name == camera.Name);
		if (cameraSettings != null)
		{
			valueOrAdd.Remove(cameraSettings);
		}
		valueOrAdd.Add(camera);
		valueOrAdd.Sort((CameraSettings a, CameraSettings b) => a.Name.CompareTo(b.Name));
		persistenceService.Persist(settings);
	}

	public void Delete(string pose, CameraSettings camera)
	{
		pose = Extensions.GetValueOrDefault(settings.PoseToPose, pose, pose);
		if (camera == null || camera.Name == null)
		{
			return;
		}
		List<CameraSettings> valueOrDefault = settings.PoseCameras.GetValueOrDefault(pose, () => (List<CameraSettings>)null);
		if (valueOrDefault != null)
		{
			CameraSettings cameraSettings = valueOrDefault.Find((CameraSettings x) => x.Name == camera.Name);
			if (cameraSettings != null)
			{
				valueOrDefault.Remove(cameraSettings);
				persistenceService.Persist(settings);
			}
		}
	}

	public List<CameraSettings> GetCamerasForPose(string pose)
	{
		pose = Extensions.GetValueOrDefault(settings.PoseToPose, pose, pose);
		return settings.PoseCameras.GetValueOrDefault(pose, () => (List<CameraSettings>)null);
	}

	internal CameraSettings NextCameraForPose(string pose, CameraSettings current)
	{
		List<CameraSettings> camerasForPose = GetCamerasForPose(pose);
		if (camerasForPose == null)
		{
			return null;
		}
		if (current == null && camerasForPose.Count > 0)
		{
			return camerasForPose[0];
		}
		int num = camerasForPose.IndexOf(current);
		if (num < 0)
		{
			return null;
		}
		if (num + 1 >= camerasForPose.Count)
		{
			return null;
		}
		return camerasForPose[num + 1];
	}
}
