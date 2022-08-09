using Bhaptics.Tact;
using System.Collections.Generic;


//---------------------------------------------------------------------------------------------
namespace Bhaptics.Tact.Unity
{
    public class BhapticsProject
    {
        public Track[] Tracks { get; set; }
        public Layout Layout { get; set; }

        public static BhapticsProject ToProject(JSONObject jsonObject)
        {
            BhapticsProject project = new BhapticsProject();
            var trackList = new List<Track>();
            var tracks = jsonObject["tracks"];

            foreach (var tJObject in tracks)
            {
                var track = Track.ToTrack(tJObject.Value.AsObject);
                trackList.Add(track);
            }

            var layoutValue = jsonObject["layout"];
            project.Layout = Layout.ToLayout(layoutValue.AsObject);

            project.Tracks = trackList.ToArray();
            return project;
        }

        public JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            var tracks = new JSONArray();
            foreach (var track in Tracks)
            {
                tracks.Add(track.ToJsonObject());
            }

            jsonObject["tracks"] = tracks;
            jsonObject["layout"] = Layout.ToJsonObject();

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class HapticFeedbackFile
    {
        public BhapticsProject Project;

        public static HapticFeedbackFile ToHapticFeedbackFile(string jsonStr)
        {
            HapticFeedbackFile feedbackFile = new HapticFeedbackFile();

            JSONObject jsonObject = JSON.Parse(jsonStr).AsObject;
            var projectObj = jsonObject["project"];

            feedbackFile.Project = BhapticsProject.ToProject(projectObj.AsObject);
            return feedbackFile;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class Track
    {
        public HapticEffect[] Effects { get; set; }

        public override string ToString()
        {
            return "Track {  Effects=" + Effects + "}";
        }

        internal static Track ToTrack(JSONObject jsonObj)
        {
            Track track = new Track();

            List<HapticEffect> effectList = new List<HapticEffect>();
            var effects = jsonObj.GetValueOrDefault("effects", new JSONArray());
            foreach (var effect in effects)
            {
                effectList.Add(HapticEffect.ToEffect(effect.Value.AsObject));

            }
            track.Effects = effectList.ToArray();

            return track;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            var effectArray = new JSONArray();

            foreach (var effect in Effects)
            {
                effectArray.Add(effect.ToJsonObject());
            }
            jsonObject["effects"] = effectArray;
            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class HapticEffect
    {
        public int StartTime { get; set; }
        public int OffsetTime { get; set; }
        public Dictionary<string, HapticEffectMode> Modes { get; set; }

        public override string ToString()
        {
            return "HapticEffect { StartTime=" + StartTime +
                   ", OffsetTime=" + OffsetTime +
                   ", Modes=" + Modes + "}";
        }

        internal static HapticEffect ToEffect(JSONObject jsonObj)
        {
            var effect = new HapticEffect();

            // TODO
            effect.StartTime = jsonObj.GetValueOrDefault("startTime", -1);
            effect.OffsetTime = jsonObj.GetValueOrDefault("offsetTime", -1);
            effect.Modes = new Dictionary<string, HapticEffectMode>();

            var modeJson = jsonObj.GetValueOrDefault("modes", new JSONObject());
            foreach (var mode in modeJson)
            {
                effect.Modes[mode.Key] = HapticEffectMode.ToMode(mode.Value.AsObject);
            }

            return effect;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();
            jsonObject["startTime"] = StartTime;
            jsonObject["offsetTime"] = OffsetTime;

            var modeObject = new JSONObject();
            jsonObject["modes"] = modeObject;

            foreach (var hapticEffectMode in Modes)
            {
                modeObject[hapticEffectMode.Key] = hapticEffectMode.Value.ToJsonObject();
            }

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class Layout
    {
        public string Type { get; set; }
        public Dictionary<string, LayoutObject[]> Layouts { get; set; }

        internal static Layout ToLayout(JSONObject jsonObj)
        {
            var layout = new Layout();
            var type = jsonObj["type"];
            layout.Type = type;
            layout.Layouts = new Dictionary<string, LayoutObject[]>();

            var layouts = jsonObj["layouts"];
            foreach (var key in layouts.Keys)
            {
                var arr = layouts.GetValueOrDefault(key, new JSONArray());
                var layoutObjList = new List<LayoutObject>();
                foreach (var layoutObj in arr)
                {
                    layoutObjList.Add(LayoutObject.ToLayoutObject(layoutObj.Value.AsObject));
                }
                layout.Layouts[key] = layoutObjList.ToArray();
            }

            return layout;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();
            jsonObject["type"] = Type;
            var layoutsObject = new JSONObject();

            foreach (var layout in Layouts)
            {
                var objArray = new JSONArray();
                foreach (var val in layout.Value)
                {
                    objArray.Add(val.ToJsonObject());
                }
                layoutsObject[layout.Key] = objArray;
            }

            jsonObject["layouts"] = layoutsObject;

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class LayoutObject
    {
        public int Index { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        internal static LayoutObject ToLayoutObject(JSONObject jsonObj)
        {
            LayoutObject layoutObject = new LayoutObject();
            layoutObject.Index = jsonObj["index"];
            layoutObject.X = jsonObj["x"];
            layoutObject.Y = jsonObj["y"];

            return layoutObject;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();
            jsonObject["index"] = Index;
            jsonObject["x"] = X;
            jsonObject["y"] = Y;
            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class HapticEffectMode
    {
        public FeedbackMode Mode { get; set; }
        public DotMode DotMode { get; set; }
        public PathMode PathMode { get; set; }

        internal static HapticEffectMode ToMode(JSONObject jsonObj)
        {
            var mode = new HapticEffectMode();

            mode.Mode = EnumParser.ToMode(jsonObj["mode"]);

            mode.DotMode = DotMode.ToDotMode(jsonObj["dotMode"].AsObject);

            mode.PathMode = PathMode.ToPathMode(jsonObj["pathMode"].AsObject);

            return mode;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["mode"] = Mode.ToString();
            jsonObject["dotMode"] = DotMode.ToJsonObject();
            jsonObject["pathMode"] = PathMode.ToJsonObject();
            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class DotMode
    {
        public bool DotConnected { get; set; }
        public DotModeObjectCollection[] Feedback { get; set; }

        internal static DotMode ToDotMode(JSONObject jsonObj)
        {
            var dotMode = new DotMode();
            dotMode.DotConnected = jsonObj["dotConnected"];
            var feedbackList = new List<DotModeObjectCollection>();
            var arr = jsonObj["feedback"];
            foreach (var val in arr)
            {
                feedbackList.Add(DotModeObjectCollection.ToObject(val.Value.AsObject));
            }

            dotMode.Feedback = feedbackList.ToArray();
            return dotMode;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["dotConnected"] = DotConnected;
            var feedbackArray = new JSONArray();
            jsonObject["feedback"] = feedbackArray;

            foreach (var dotModeObjectCollection in Feedback)
            {
                feedbackArray.Add(dotModeObjectCollection.ToJsonObject());
            }

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class DotModeObjectCollection
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public PlaybackType PlaybackType = PlaybackType.NONE;
        public DotModeObject[] PointList { get; set; }

        internal static DotModeObjectCollection ToObject(JSONObject val)
        {
            var obj = new DotModeObjectCollection();
            obj.StartTime = ParseUtil.GetInt(val, "startTime");
            obj.EndTime = ParseUtil.GetInt(val, "endTime");

            obj.PlaybackType = EnumParser.ToPlaybackType(val.GetValueOrDefault("playbackType", "NONE"));
            var list = new List<DotModeObject>();

            foreach (var jsonValue in val["pointList"])
            {
                list.Add(DotModeObject.ToObject(jsonValue.Value.AsObject));
            }

            obj.PointList = list.ToArray();

            return obj;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["startTime"] = StartTime;
            jsonObject["endTime"] = EndTime;
            jsonObject["playbackType"] = PlaybackType.ToString();

            var pointList = new JSONArray();

            jsonObject["pointList"] = pointList;
            foreach (var dotModeObject in PointList)
            {
                pointList.Add(dotModeObject.ToJsonObject());
            }

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class DotModeObject
    {
        public int Index { get; set; }
        public float Intensity { get; set; }

        internal static DotModeObject ToObject(JSONObject jsonObject)
        {
            var obj = new DotModeObject();

            obj.Index = (int)ParseUtil.GetInt(jsonObject, ("index"));
            obj.Intensity = ParseUtil.GetFloat(jsonObject, "intensity");

            return obj;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["index"] = Index;
            jsonObject["intensity"] = Intensity;

            return jsonObject;
        }

    }

    //---------------------------------------------------------------------------------------------
    public class PathMode
    {
        public PathModeObjectCollection[] Feedback { get; set; }

        internal static PathMode ToPathMode(JSONObject jsonObject)
        {
            var pathMode = new PathMode();

            var list = new List<PathModeObjectCollection>();
            foreach (var jsonValue in jsonObject["feedback"].AsArray)
            {
                list.Add(PathModeObjectCollection.ToObject(jsonValue.Value.AsObject));
            }

            pathMode.Feedback = list.ToArray();
            return pathMode;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            var feedbackArray = new JSONArray();
            jsonObject["feedback"] = feedbackArray;
            foreach (var pathModeObjectCollection in Feedback)
            {
                feedbackArray.Add(pathModeObjectCollection.ToJsonObject());
            }

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class PathModeObjectCollection
    {
        public PlaybackType PlaybackType = PlaybackType.NONE;
        public PathMovingPattern MovingPattern = PathMovingPattern.CONST_TDM;
        public PathModeObject[] PointList { get; set; }

        internal static PathModeObjectCollection ToObject(JSONObject jsonObject)
        {
            var collection = new PathModeObjectCollection();

            collection.PlaybackType = EnumParser.ToPlaybackType(jsonObject.GetValueOrDefault("playbackType", "NONE"));
            collection.MovingPattern = EnumParser.ToMovingPattern(jsonObject["movingPattern"]);

            List<PathModeObject> list = new List<PathModeObject>();

            foreach (var jsonValue in jsonObject.GetValueOrDefault("pointList", new JSONArray()))
            {
                list.Add(PathModeObject.ToObject(jsonValue.Value.AsObject));
            }

            collection.PointList = list.ToArray();

            return collection;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["playbackType"] = (PlaybackType.ToString());
            jsonObject["movingPattern"] = (MovingPattern.ToString());

            var pointListArray = new JSONArray();
            jsonObject["pointList"] = pointListArray;
            foreach (var pathModeObject in PointList)
            {
                pointListArray.Add(pathModeObject.ToJsonObject());
            }

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    public class PathModeObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Intensity { get; set; }
        public int Time { get; set; }

        internal static PathModeObject ToObject(JSONObject jsonObject)
        {
            var obj = new PathModeObject();

            obj.Intensity = ParseUtil.GetFloat(jsonObject, "intensity");
            obj.X = ParseUtil.GetFloat(jsonObject, "x");
            obj.Y = ParseUtil.GetFloat(jsonObject, "y");
            obj.Time = ParseUtil.GetInt(jsonObject, "time");

            return obj;
        }

        internal JSONObject ToJsonObject()
        {
            var jsonObject = new JSONObject();

            jsonObject["x"] = X;
            jsonObject["y"] = Y;
            jsonObject["intensity"] = Intensity;
            jsonObject["time"] = Time;

            return jsonObject;
        }
    }

    //---------------------------------------------------------------------------------------------
    internal class ParseUtil
    {
        internal static float GetFloat(JSONObject obj, string key, float defaultValue = -1)
        {
            var type = obj.GetValueOrDefault(key, defaultValue);
            if (type.IsNumber)
            {
                return obj[key].AsFloat;
            }

            if (type.IsString)
            {
                return float.Parse(obj[key]);
            }
            // wrong
            return defaultValue;
        }
        internal static int GetInt(JSONObject obj, string key, int defaultValue = -1)
        {
            var type = obj.GetValueOrDefault(key, defaultValue);
            if (type.IsNumber)
            {
                return obj[key].AsInt;
            }

            if (type.IsString)
            {
                return int.Parse(obj[key]);
            }
            // wrong
            return defaultValue;
        }
    }
}
