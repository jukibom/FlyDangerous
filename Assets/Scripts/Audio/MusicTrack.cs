using System.Collections.Generic;
using JetBrains.Annotations;
using Misc;

namespace Audio {
    public class MusicTrack : IFdEnum {
        private static int _id;

        public static readonly MusicTrack MainMenu = new("Rock-Your-Body", "Main Menu/rock_your_body_loop", "Main Menu/rock_your_body_intro");
        public static readonly MusicTrack Juno = new("Juno", "Levels/juno_loop", "Levels/juno_intro");

        public static readonly MusicTrack BeautifulCatastrophe =
            new("Beautiful Catastrophe", "Levels/beautiful_catastrophe_loop", "Levels/beautiful_catastrophe_intro");

        public static readonly MusicTrack DigitalBattleground =
            new("Digital Battleground", "Levels/digital_battleground_loop", "Levels/digital_battleground_intro");

        public static readonly MusicTrack ChaosAtTheSpaceship =
            new("Chaos At The Spaceship", "Levels/chaos_at_the_spaceship_loop", "Levels/chaos_at_the_spaceship_intro");

        public static readonly MusicTrack Hydra = new("Hydra", "Levels/hydra_loop", "Levels/hydra_intro");
        public static readonly MusicTrack Hooligans = new("Hooligans", "Levels/hooligans_loop", "Levels/hooligans_intro");

        private MusicTrack(string name, string musicTrackToLoad, string introTrackToLoad = null) {
            Id = GenerateId;
            Name = name;
            IntroTrackToLoad = introTrackToLoad;
            MusicTrackToLoad = musicTrackToLoad;
        }

        private static int GenerateId => _id++;
        public string MusicTrackToLoad { get; }
        [CanBeNull] public string IntroTrackToLoad { get; }
        public bool HasIntro => IntroTrackToLoad != null;

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<MusicTrack> List() {
            return new[] {
                Juno,
                BeautifulCatastrophe,
                DigitalBattleground,
                ChaosAtTheSpaceship,
                Hydra,
                Hooligans
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