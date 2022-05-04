using System.Collections.Generic;
using JetBrains.Annotations;
using Misc;

namespace Audio {
    public class MusicTrack : IFdEnum {
        private static int _id;

        public static readonly MusicTrack MainMenu = new("Rock-Your-Body", "Main Menu/main menu loop", "Main Menu/main menu intro");
        public static readonly MusicTrack Juno = new("Juno", "Levels/juno");
        
        private MusicTrack(string name, string musicTrackToLoad, string introTrackToLoad = null) {
            Id = GenerateId;
            Name = name;
            IntroTrackToLoad = introTrackToLoad;
            MusicTrackToLoad = musicTrackToLoad;
        }

        private static int GenerateId => _id++;
        
        public int Id { get; }
        public string Name { get; }
        public string MusicTrackToLoad { get; }
        [CanBeNull] public string IntroTrackToLoad { get; }
        public bool HasIntro => IntroTrackToLoad != null;
        
        public static IEnumerable<MusicTrack> List() {
            return new[] {
                Juno
            };
        }

        public static MusicTrack FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static MusicTrack FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}