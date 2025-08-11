using System.Collections.Generic;

namespace Core.MapData.Serializable {
    public static class LevelDataHelper {
        public static Dictionary<string, string> OldMapLookup = new() {
                        {
                "3e4dca7ecb638c3958c5bbdc3851fb5a48e2fbeaa6b3bb38f74ed4761574aee8", "b9b6a0e268fb76964dfa2dbc266812ab642cc4c9838287f7273fee24029aed52"
            }, // You have to start somewhere
            {
                "f8b73d1a8a8c5233dcf4ac653583c2dfdbd3d362fb43507bcdd6b2d1153c167b", "b51b9ef381c07055e2065ee319cd4e9611dbf20a5574a082d83c1e09e87f0468"
            }, // A little verticality
            {
                "c4f56a38504cd0b3a843c73d9b6904ead359849e471b1652629efa38c3f69122", "b7c849a463e4973ffdd9340a8413ebde437e11642b7194fe9976e1772b2ff30b"
            }, // Ups and Downs
            {
                "f58b99383b7af767cdcac9f7dbf15b96e54aa5b06e12f9db970f3c7009902710", "0ffdeed0899c63d0dfa4e339842764b4d73bd90ebf49f5ff6e34620ca290b357"
            }, // Hold on to your stomach
            {
                "fd4993399004b35be5b91bc4a99a17c856ea1f7f08f2ec0960fb3a048a944dd6", "074c974412f5a49c71afca8c772edf83494f8e9cf80e9218868085f4c30e0ead"
            }, // Tiny Trial
            {
                "d59e18ac81901a4b705479eb96f9aa7a6f48052626b525392963baa2345188c1", "e2e7bc2be85a233138f0630c4c0c002933df3da0dc7690a7cf60695f13f1ab9a"
            }, // Ramping up
            {
                "032d33721a5210498393a3cafb49c9955649ffd08cb0bce2afa3e96ff6539885", "f9e77ad7b89fe7f80027263356927c46181417e97a4cde0ddb1c9283683398c2"
            }, // Around the Station
            {
                "a76ff9f6ef82778898f8f70f0539fe9347052d71fb4455add00b429a20d06ef9", "100d1fb8d0b5809d89217157f11e33572ef8fbf8ad2c7324173398dbc4a284a4"
            }, // Around the Block
            {
                "1c755e8cf10b4c5ab7b2b7164d7b04b484e2b3c568bde01d02a5fa3104daef5f", "28717e0173d79b83061279c4e9c2e5f10488b6bdb36688255f7c90d244d3a4a2"
            }, // Speed is Only Half the Battle
            {
                "80c50753287ca5a9234cf8b395653721319f4d729c799d6e84bedb990d20e332", "8657048592eee0ee04196716df4cccfbbed733f2b3b4a23d5fa3bb1e8a2172d3"
            }, // Death Valley
            {
                "fb36a2e467313388fd92d273143ec30f0d9802da0f80ee0fb9c08026e09c07f6", "1d12b7b6cf492877de0bc55a1503101be6aad12bcb054086883d2e2c88126839"
            }, // Snake
            {
                "381772b47cb35776666e7ed59c0552c400a0a77167393dd819eefdcf5e73cd57", "e2f58025933d2dfb992e96b62b97abd30dbfacdd8f67bcb562249b818c367911"
            }, // Corkscrew
            {
                "04f60ea9ccac58b9b13cd6a2bda42329338f0140e6b2d3a3e4bd168d20b30969", "c2ab1591b9fa43f6cde619ed83f547e3a8a7b440cda62a51aebbf2d1968df32a"
            }, // Sightseeing
            {
                "93b383bdc5b006a7cb3a136b7775650a5c06a8ceeeab2fb87809d2c64e546eb7", "5c6b273d58aa609e7fbf68b420b845fe3ee683e0eaa7171693aabf1fc028d283"
            }, // Yeet
            {
                "daf178b9e629c5c45af6cd1bd4afbf8fcccd8f7b44ba9fe91dd065d304839838", "b94db94e6e0db288a78ebce957a3e2d032b82b280bd7723ae29800d7f589ffcd"
            }, // Desert Dash
            {
                "07b44ca4636d2178cef3d8c4cd58953c6102d4fe616233fb157d6ffe71e157c3", "50c43dbaab561bf0a91c3671084b2e75f62272fee5023ba0c52d3b0ccf32790c"
            }, // Coaster
            {
                "9ca2386214c582868993071f6a419b4d457fe12c6e4abe98a4b77cecf6be9ae3", "8934f87773d34f7cdd6e8089d81d492989219ab2ac055bc612d9a67e0a5878c3"
            }, // A Little Dip
            {
                "5cc34b6a61ab690ac1fd6cbf3902016f6f84cfdacb025fc8724a460ed771e380", "f208e73c10c7c29e8d60c1790bcc5391fcb8da25ec0bf3084af9619081bf2223"
            }, // Marsh Marathon
            {
                "fe008d53741ee9aede091e5a824ccbf5e4f95e0e9c0610700dd70769e1b51212", "790dbcc3528b52fbf10adbd592b9a89f52621e9dbded0f44b63b0371053479ac"
            }, // You Might Wanna Hold Back a Bit
            {
                "ead6f9407d53e2bf6e2f76202172fd0ad0ef309c7275f5ed3ccc633ef7fcacd1", "46f61ce852c1f5673ab384ac2c433f490eaf5d2b2eca7ff7d2f0e3d60034f196"
            }, // Tight Squeeze
            {
                "acb91ac6fc2b0363daed87820cbb9bf28ea1a8c24f631f30469a161cea84510d", "2c1df43c456f66e281e3794756ac92ec31d1fe0ae82990ce0461ef5bec2e6fd9"
            }, // Thread The Needle
            {
                "3c70d6f8f1c475a80b1cab7eacec7b5a5760b389761fbda42a20ca309ce31ad9", "de0c808bf7ccc69fe29676564e3c34325b870fb2e708e319b115378c87e51dbb"
            }, // Chute
            {
                "ea0947155ffae6b20fff52a225cdf25cca71b2891ba037b78b9a1537c7a3fea1", "b89457940d87fbc2c10063a2f4e4b784d89c4441c021432a21720591f4d2d1f1"
            }, // Twists and Turns
            {
                "0ab98ad6e904c7191d0c4f896fef90c8cb566fd7347e9f5d200f7a13ed47bea8", "e8821db37b24269de8077c270bdc15f1c9177c24a2ed4758aa659c5f49e49ccf"
            }, // Mountain Spiral
            {
                "0cd913b309182a0264ead137c3c227661757bd8c743f54ed8912fd48353dbdb4", "78f8b39387fd7da0b81774b41d14fe514d2ff71ae5d818a99d9fe7cd417ae6a3"
            }, // Loop-de-loop
            {
                "85600dfab820d0ec288e38e866537feef105e8e218e92f646b80b1b0eeae72e5", "7a2e21e71455bb07b5029486e824bca49f1d00d0a2354fe195b0d8cdcde8dc2c"
            }, // Crest Loop
            {
                "f549b9be9a27b744ec6b76c3af608463d6988ca38786069563c6d2beaed79fc6", "cfdf89e9a69c428f029feacbbbefcad6f4d0fce233bc9317769cbbdeca67415f"
            }, // Slalom
            {
                "f16f67353a662d4c8f8a86e30f8b34274345f046b6752ce9f5fece373b2d3b3b", "5f963aa651e93fb019ecbff9498d677193855b04a25819457aa4b411183160c2"
            }, // 
            {
                "c5e61699eb54cf597622acbb5934d06f00470789a805a2bd7d5f42231a1935d2", "7c1e52c2932d8a6b8df1c33cfc5cda02d9dad0b552c09725c115d51130054546"
            }, // Island Hopping
            {
                "cfb5825ac7a15b6ffb0e517af7a5808a27c7f24c0f79aa499c3760c2a42b05ce", "73cd2df744c7268d56324c5b4f8df82f61fbd6d3b98207bba35a94df7d6cb18c"
            }, // Labyrinth
            {
                "d8a6bbaeb93c26d107c5608d642c25df8ecbdc851130826a994436a5aa283c5f", "7cb8a97784df5f5a3615c27e62bf3bf026679ba84afa2ae6634dc79cad3e09e7"
            }, // Fresh Hell
            {
                "ab333f60d4e2ebf30f298c5116859a7af46340d22fc1e36897e5696e4cdc3775", "d603aa1d27868096bc58e4ea1c5dc04af889e8c240c7858e7cc5bce7e8d8423d"
            }, // Around the Station
            {
                "760db944ea99bc1930e577d9d6629ee5d455da7c39519e481b318883614538bd", "2bafc759ac2e3fca5742400dc524fdd6faf3204a66479d9a85691cabe37f8213"
            }, // Coastline Circuit
            {
                "d6a3cde1f2a6a310e34767f51a15e9c11cb3dc88d0c53f1f5296e5d36dfc7975", "4ad1a92829ce54b8ab7c72d8c29cb3c7e41633289e1e3ffe4ee0eec29dfc462c"
            }, // Slipstream
            {
                "303346b80650992d8594224a22cb7b57fdc0027859816c5e80f67d144ed7e493", "1c9e30cdeec46fd888ab061c56e367aff30452098fa3d99f7e35179ed8626c29"
            }, // Speedway
            {
                "f7cf2c15adbf8b0b265f29d50fb579554bffce3bfbf26798b5cbc45164a9e79b", "f6c08bdf3a1f6c8918251d4c1ace722e664fcf14c8412bb03ce39062439ba6a1"
            }, // Long Haul
            {
                "5bfa9dc69bca3d41d9a12d4d38730e2db77941195c9361c711ab7ee33750d651", "2f6af5b1233ad1dc77810c314d08a89ecc2a62592bf0b509b9ec095170325218"
            }, // Decisions decisions
            {
                "e0c369f9634ce0c6a41dac8097b74ceb8fe22c4a1231661f077dcd271df57382", "85add39ccb8579a8a461cca667f5a15ad709a08d3ee1fc82fddd929d05189847"
            }, // Playground
            {
                "9ef9d3cd0a6527665cbdff39efcf647406265a3e115f9649c5c089b3c151d300", "b8bc0131e9c7afda485fe9c0c9950fc422cd61d43768aceb2332b395010e91de"
            }, // Highways
            {
                "ecbee95d0b3c52369615b3e8b2225ea889b37999a5a633fc65a3557cc7d5d744", "59edba84985793d319370ee42f649631fd21f317e32fc0bbab5b179425bb2dd3"
            }, // A Gentle Start
            {
                "fdb2852cf94e6e7aedfa24ab8225e085ce77cad2be0eb1dc6b4c12c1ef0461d3", "7dc9ec3fd8561551fef7c3013222ec66a08e9f920aea39c8c60a867d46069b21"
            }, // Limiter Mastery
            {
                "f56b0f4829302a803e676e2cdeedca280d26f7f5c00d60f57a954ea840a2ceb9", "23f261ee8cd18fd2db02e9738b3f4100867de776c010e5ded51dd82c00149a0d"
            }, // You have headlights, right?
            {
                "7161561fa2e8c659681fced9e6d03e6a92930615302f6207308692e7f4f43899", "8b7f9f7b4d0b5a29f6b6bc3a3778424f45d85baad750df2d0df68da95e08768a"
            }, // Hide and Seek
            {
                "f238d0c900752da4859ca232f4524e07e53673adc610f63b3115d4b8aefc0467", "9332e14624443f75fd96174e19b1c9601661a00ae35dbe8f066425e2ac379457"
            }, // Agile Mountains
            {
                "d8dee4665e550e7f434d4b8013cce1f04c309e579ee1caa3a06a3a0a6c371cb1", "446cbbe8bb4eb1f883581f9cf2e57644049d7df1912a3f5b68c9a6c0497737cf"
            }, // Black Diamond
            {
                "e42aa2f1d70dced22838a791bff8e4e40b5ebfc81bd5ba2235202b552792ea14", "f9c55b3bf10eaac1d9808a23e560fb90102e5b6c1884e9339d7ff0b685a012c8"
            }, // Hanged Man's Peak
            {
                "b23436b02b4bd6fec24c3d9cc69fb54c1c75bbf7eff00f6135813b5bacbd227d", "179df49357cdd3747c9f6b64874fdbc1e6f98c3515045c86476552c0acdefebc"
            }, // Kung Fu Fightin
            {
                "015acc05a1eb87afab3c0e17ba3617f67160e8d2b27166a732f6bba1c0ce844f", "a736ffd692a0fa1e4ea76c6d731099830bc0e2daf325a874d09e7dbc137af995"
            }, // Lake Turns
            {
                "694c479d3056a181e4f5049131a003183908f6c43e2bac870a606062b64cba44", "120fc850b4c98b194524e2135c4af157536bd09d4f2c7578a24ef9d8bb21f281"
            }, // Limiter Mastery MkII
            {
                "7d3980415b8c86c49884b4e027e82a4602cf2635686a11cf0c2c2649dcda71c9", "ff6dff859d3d90dc4403a6d6f7fb3d75dd8dcb8a322bc5366949be2ad1fd9e57"
            }, // Looks familiar
            {
                "a150e7f4e801aff6f3c6083138d3dc2e6f272c96cde4821f66e5b62bb9a7a1c1", "c8ef04a82ea6420af24ce5b7d700b2e3e49de113d222e4e2fe58fa7920ef8b9d"
            }, // Noodling around the station
            {
                "06ef1388d37cce0ec3df3742bb92c7072260c96ef3ea4a97eb36f9d30391ed71", "fb28badc74ef20c6e6ad746b25143f4595103adc57571227db9b2f372d9d49d2"
            }, // Nova's track
            {
                "f7db4a5a81ceb2d943fb80f125f1bb7c8cafed5eff083ebb7dcb7e191cbd62cc", "531b1892c2ece19806fa8684f6eb6bd608ebbb02b94a3ba4c3313df8841fa76a"
            }, // Roller Coaster
            {
                "29f52abb02c01f3a8eec41e97c2dcd4c3de7609b94b30be893895f087c5b45a5", "a6acc6fe3ac219f88cbd3fa4889a35d2c5281879f1cb77a9dfbf6b89b29228b5"
            }, // Slippery Snake
            {
                "a2b4ccec6edb74ed12c1f5f1afde03c79e9ad471e5805db00fadcf81d3b04201", "b14ffc31418dd56eaf121cca27a1243388af53bfa1d1189f951dfd487e0c2ff9"
            }, // The Ground
            {
                "46ef1a2b2e7647432f65d8c2a430e404da170671c7cc239eaeeea82840034e1d", "4ffc6e47535a4008d33cc9277d6fb8907fb9a9692edd109c966872f5f7beaefa"
            }, // The Island
            {
                "710e322e046c311f6933a1bdf21519d047736976a0190646b62951b1bc87a234", "e00ca9e5242b16617a660c703aba5f5e05fee2b8e19b3ab712082d49a9ab5c6d"
            }, // The Trail Run
            {
                "f06fa14fe525d3ff73255beea598f47e107d76cef35bba4eee21fa58990fe539", "97ea3592f8cc285004ca51ba0dfc31fc6218e28efba1102f80303b848cf6bd3f"
            }, // The Walls
            {
                "aab6bcddff688ca0d00772f092906d0160ea3fe42bfc13e4dd5fbb4768226d92", "0febf54c41bec70ebf2ae9f484733a998b10a4c598108b872002b4edf2e64814"
            }, // Titan River
            {
                "d1f7724a283733ceef1bf513391ba7425b7b0d4d3e516ab2640496460427ca09", "3b1c27fb3ff1e74f9172874f2ca1e997f6a21947fcc6c820d585f35e2cf28ecb"
            }, // Trench Run
            {
                "4b8cc32d6514b04c3502af49a0b1e63be6ac98110a91033f863e46b6422f112d", "099b443568b2e9519e0d24c143e7bf4809931a2f86a95e9ff5acc5834a047ada"
            }, // Twisted
            {
                "5ae059e1a584151a6b9f7f71fbfab7a04d5616661a393a9254eafb38637e28b0", "c2f7365441d1d0aaf51fc378576e490a66ec94dbcb4c4502f65b1c4486b9e36a"
            }, // A Gentle Start uncapped
            {
                "3277fb3461edfd39a53812df6ffd7577d716f27e51d7893291b80cee0cbe82d0", "30208c0d5366fb6801eb130681adebba8701c94dc64b4b74c0c8d5cc7d4af4ff"
            }, // A Little Dip uncapped
            {
                "d9adaf60c67bd4200299a5e4dbfcb232c4de0789e1d7cb9b6c17a0a48d63c756", "945008084642cacfd9075cc7688c9fba03f5c22ef8d97160546362526c17dd1a"
            }, // A little verticality uncapped
            {
                "97fbb30f1596b91edd87362492a56b746018d979ac62371246f6242862aece2e", "b83585ab34e778c26156c276502b418fd145857a784a8b0a84dbb368803adad1"
            }, // Around the Block uncapped
            {
                "498c910fc627be35c11dbb630f82e83876732911a6a1a5771726db86669e4f5e", "115993a0f52ba5efdb316f0cea18d973fce071146b470a9e61501d239052bd47"
            }, // Around the Station uncapped
            {
                "4dcf3dd450d261f62b7f9a215c71c0468a2338514da401f01f17d46ae757523c", "99d136d64345923788343c701042f2ef990debfb07ee5800663ac2ab71d505c9"
            }, // Around the Station uncapped
            {
                "76b09ed91a5f82363486a452b851e7ff19e9024ae7cb29a72e4e82a7d27c50ca", "ef51cb8bdba90ee27cba60b965015ba708ef6e58871d59ab1dd91d0626ad3e1c"
            }, // Chute uncapped
            {
                "785ae510f2fe60cc9d30c70b94b1ff39b605f97f1a26c551830b147095f91b7e", "0516ede09baa47878f47c06d9635913b09b03ddf9ed87eeb5f3d15663d51e098"
            }, // Coaster uncapped
            {
                "73d2ff671ae900663703a30e1c787c16ea762fcc3fca70af496c56fb71edfefb", "c09d9dd65a7a8b7bece6350f661db6fa040d19fba7cd8751df0546ca81b0b68e"
            }, // Coastline Circuit uncapped
            {
                "82b238cd7453114d0ebcbf03eac4f30777d612524d33d8b485f934a1e0fc2211", "d4340f459c2aaf7dff11798aff8fe30f9b2768be5689c1724feeb7245ec9b44d"
            }, // Corkscrew uncapped
            {
                "4a44e8b19d45c48f6ec851bfaa379821840187aea546244b4a902c95c5746231", "35a3cae14b345ce9c5fa82bef52624db42626aa973d0d06cdc4b02919ecd928d"
            }, // Crest Loop uncapped
            {
                "127c5cec7f5d62aca3f6590f12e58495c7a6aee9ccdbdb17f32df6bf3cf8dbe9", "38830bc5c58907a127ea57a9e7222d059cd562902c75d6004c5efd54c0917c7e"
            }, // Death Valley uncapped
            {
                "c5262598d276f7c30873fd44344a151d68e0559ebc141d2fbef24de61972d2f0", "e53cc48bcc3876796e9e45e7ff85597e407f4eb9bec17dc7c6082a2959995502"
            }, // Decisions decisions uncapped
            {
                "0cb5a9faff596fbcb15686fc579201b043e381a0ceaa19613f5f9f43042be6a7", "0a5f5b37879171a087acf0d14d470e9ec01c6fd81306bd14c640ede160a19805"
            }, // Desert Dash uncapped
            {
                "1e202650fc6a7907dd7699a94463ee4da2b32bc5aa4dd75f87911a1d47916013", "2f785516ae672527122a3718fbcb813adf04ddb5338979ab4c87b816ce3dda27"
            }, // Fresh Hell uncapped
            {
                "1687b00bfc03eae1fdd89d4f410809fc8d25261731f922191c8efc8b71df8754", "573d3df788bf3c7cd3326cbc0a37bc60a6c5f612b384c0a43e59aa099d5a0128"
            }, // Hide and Seek uncapped
            {
                "0a038dc4fa75f5651e802bb3769934b9fefa7674fb93f15aad25f216e890f9bc", "1f4dc8c3d8187ee7a93d9c74be059f9c4bfa5ea918ff98c6e05084ec1b440f92"
            }, // Highways uncapped
            {
                "9c0692d1a863e731d8d6056ae278ff8ec717caea0443aea6913af2f0545f05a6", "bde9f0c718480eb8149bd4c16304af869d216e2b7877de5fbd4e483e2ab52917"
            }, // Hold on to your stomach uncapped
            {
                "2f1de0cf9cb6c431006d85a5c79be82a27430ecef69a06c90d48b383906bad38", "44e21e977562963a8bef36ffe304081b78d8615a68d1d2d7518ebaccdfe82cf9"
            }, // Island Hopping uncapped
            {
                "b86703c84ee57904ff4ed8a948f686b314f643f1244bba16cc1c3361d2e09cd7", "8a8b4885f25bc4db7a0aa5a0ca847b505d9d0b15dc46cabe8afb7e8fac153175"
            }, // Labyrinth uncapped
            {
                "54977cee22a7683eae274f2f7ac01e31d5d6526cee601064b5be80ce0317ef37", "7f01a967f5e1ec9894b9864fe251ce07515aed001748a277a51b798a39ec705d"
            }, // Limiter Mastery uncapped
            {
                "7bd653d02df387e1744d5eca3efc7a2de152d77ac41a19e14426964b578261ed", "bd22ecf17e463d1b833bba1637a28caee51179ea43eab69c66ced257f9d2895b"
            }, // Long Haul uncapped
            {
                "5db872e29bb43f8d827af717cc50a12a418f5597ed5c8a04c6b98aee082271db", "52a1a5d7d3fdeba052c17789fa4d630a177441c9bd3f57fb5c368417e0b5efd0"
            }, // 
            {
                "91932e9e15c2638a310c959f45e8b17c1a1d1c2e23c3b0567762049d3a94cd16", "492c7981a683b1b57400bf633c4884dd212686cd2dfc4b9aef1bd62506f5480a"
            }, // Loop-de-loop uncapped
            {
                "fe3c2f0d43376d98f588c2b5b28ff35a0186c5c340995865c0135e73167fddce", "e44359189ee2be026959f45c8e012eb3637650d8adeaa35657f5ff001dbd40c5"
            }, // Marsh Marathon uncapped
            {
                "1b992c3b11ae8ff4d70b66718477ac40df9e7d0220e621f3c945a3c51bcce61a", "f80874d7691ceff1f78ea31dcab9d1cdaa1d7a1228cc93fb3a70105c558c0a80"
            }, // Mountain Spiral uncapped
            {
                "0bbe4be10abbbc90c011db73d5a2ca30eef6d919548d2357977c380e5b946d3d", "e2362703f79a9e0bb35190232f2410a727fd9a2b13b30170eafbdeb11ec99685"
            }, // Playground uncapped
            {
                "52a21eb3346019413828cd026c8f2bd6adeb3f1bfada57b8ab232be2e1904747", "6f71b7fed6dd2558a9756da71a20ee0eff007381f5acbd71e7b501c3d24ff22c"
            }, // Ramping up uncapped
            {
                "6cae5c725ef90d3154473d54de81a201ab3de330b841166f9e307784bb1f4060", "a666e5d0df6112fd6626b6d6c9ac3c1970b81161fab0811c21049459cc16ad12"
            }, // Sightseeing uncapped
            {
                "edcd2fe513f0775736872103c9eb8578f0a876c85437a14a36d6c128bb1124bf", "28d8aa7bb91e3c5b428b2b38b769deb1b419892c78bff53a7bdcc7e16a50fb70"
            }, // Slalom uncapped
            {
                "274f3508eaf88bda00fbcc307c67555f6c552b24aa6dd951c2d8474991a07823", "4159c5f66a85c127989d1b3464fcffc18a9ec1577560f29c809955c8caf14881"
            }, // Slipstream uncapped
            {
                "ecac2f617147260fe415083a71ecd2179e2cd5e74a21ab840a8dc640a9e6f0b2", "7c3562da7d29e3680874fe6ce9d61d5ab53a77b9fc5fbc6f7077fa90f489573f"
            }, // Snake uncapped
            {
                "d7e96ddffd95013b94d788a074b2a5013008fa34de11cfc811563c14e906c320", "6941084731c702954f6a76da596b0d43f7dad982ca7d694bf7289dafe83c1d3e"
            }, // Speed is Only Half the Battle uncapped
            {
                "e8b64fc6662f29a06505e233f666ee4e3432a19f6091b0c48b47826811dc8fa9", "cdb318c4f5103ffc677b48e4157b1dc0f6d3394220fa4c4f261b8a58cde2176f"
            }, // Speedway uncapped
            {
                "b10e84c5845c7d4e4826806d84a2039578447e1c8beb969927fddf9a4ea5a3e9", "46574bf1eb052b5207d975c999af614d7805bb85618f74d17a4fe3f4fcff6b40"
            }, // Thread The Needle uncapped
            {
                "2c8ac726c28cd6a9e759bef5ac271e5c51e299ad40db3bf36dac72d0a9edd91a", "58e904e32e153ed19889eaf3257238656ff859cd37389a43542874d698b25516"
            }, // Tight Squeeze uncapped
            {
                "36a430b7d64b5eae440461014aca6cbd4994ab941aa1b0db1c67111c93da2d7a", "475386e9817e473365ec9e97a1a08e92a0b6dcd167f23d04427191a77396e87a"
            }, // Tiny Trial uncapped
            {
                "4c33981733c3af8008b0fc6f0a2635d57c62926c2db907db215323c9e0c8755d", "e1d8ffff539aa5711025f1b379c761feb0123828eb9a3ccd2879d67adee89762"
            }, // Twists and Turns uncapped
            {
                "176f32cbc6e19f6646f662638ab0772d255de1212284158fade06fbc8aaccd10", "fb8ef6c1d0dc640ff1ae674704e2cd2fbcd92ed8e109d0a04d7149d7820f407e"
            }, // Ups and Downs uncapped
            {
                "4ddf30682ed80814919eac222d2efca43a142aa43378c7090b6c596d95e6a13f", "e484255f6acd5dc9863ab349fe588bf88c9f6350bc2a0535142e9d2f0cb244ae"
            }, // Yeet uncapped
            {
                "9bd6139fbe1ab996274a085957c800f11b7ab94159b11e7fa44933736324f965", "0dc635617267b5e51e0370b2f72c17c8f582b32a4a48a744981a991a18d1868c"
            }, // You have headlights, right? uncapped
            {
                "c3cde0e9d6a95eb2422e84b24858eaf60ca8eab1e24b6c4e45b8d5e7b0b68daf", "c4cacda4b5d15b8f5c28d4a9965e30c61d891d534242b46ddf02939f0b0b37d6"
            }, // You have to start somewhere uncapped
            {
                "53cfcf69a0e20eaf5bcac28aea171dc74f0bf60c5abc067087aabbb434b4500e", "76b4bdec994186a4a53709fd6f3e2271a3370a07a2907ec0f327e8b81bcf413b"
            }, // You Might Wanna Hold Back a Bit uncapped
        };
    }
}