using System.Collections.Generic;
using System.Linq;
using Misc;
using UnityEngine;

namespace Core.Player {
    public class Flag : IFdEnum {
        private static int _id;
        public static readonly Flag None = new(0, "None", "!none");
        public static readonly Flag Andorra = new(1, "Andorra", "ad");
        public static readonly Flag UnitedArabEmirates = new(2, "United Arab Emirates", "ae");
        public static readonly Flag Afghanistan = new(3, "Afghanistan", "af");
        public static readonly Flag AntiguaAndBarbuda = new(4, "Antigua And Barbuda", "ag");
        public static readonly Flag Anguilla = new(5, "Anguilla", "ai");
        public static readonly Flag Albania = new(6, "Albania", "al");
        public static readonly Flag Armenia = new(7, "Armenia", "am");
        public static readonly Flag Angola = new(8, "Angola", "ao");
        public static readonly Flag Antarctica = new(9, "Antarctica", "aq");
        public static readonly Flag Argentina = new(10, "Argentina", "ar");
        public static readonly Flag AmericanSamoa = new(11, "American Samoa", "as");
        public static readonly Flag Austria = new(12, "Austria", "at");
        public static readonly Flag Australia = new(13, "Australia", "au");
        public static readonly Flag Aruba = new(14, "Aruba", "aw");
        public static readonly Flag ÅlandIslands = new(15, "Åland Islands", "ax");
        public static readonly Flag Azerbaijan = new(16, "Azerbaijan", "az");
        public static readonly Flag BosniaAndHerzegovina = new(17, "Bosnia And Herzegovina", "ba");
        public static readonly Flag Barbados = new(18, "Barbados", "bb");
        public static readonly Flag Bangladesh = new(19, "Bangladesh", "bd");
        public static readonly Flag Belgium = new(20, "Belgium", "be");
        public static readonly Flag BurkinaFaso = new(21, "Burkina Faso", "bf");
        public static readonly Flag Bulgaria = new(22, "Bulgaria", "bg");
        public static readonly Flag Bahrain = new(23, "Bahrain", "bh");
        public static readonly Flag Burundi = new(24, "Burundi", "bi");
        public static readonly Flag Benin = new(25, "Benin", "bj");
        public static readonly Flag SaintBarthélemy = new(26, "Saint Barthélemy", "bl");
        public static readonly Flag Bermuda = new(27, "Bermuda", "bm");
        public static readonly Flag BruneiDarussalam = new(28, "Brunei Darussalam", "bn");
        public static readonly Flag Bolivia = new(29, "Bolivia", "bo");
        public static readonly Flag BonaireSintEustatiusAndSaba = new(30, "Bonaire, Sint Eustatius And Saba", "bq");
        public static readonly Flag Brazil = new(31, "Brazil", "br");
        public static readonly Flag Bahamas = new(32, "Bahamas", "bs");
        public static readonly Flag Bhutan = new(33, "Bhutan", "bt");
        public static readonly Flag BouvetIsland = new(34, "Bouvet Island", "bv");
        public static readonly Flag Botswana = new(35, "Botswana", "bw");
        public static readonly Flag Belarus = new(36, "Belarus", "by");
        public static readonly Flag Belize = new(37, "Belize", "bz");
        public static readonly Flag Canada = new(38, "Canada", "ca");
        public static readonly Flag CocosKeelingIslands = new(39, "Cocos (Keeling) Islands", "cc");
        public static readonly Flag CongoDemocraticRepublicOfThe = new(40, "Congo, Democratic Republic Of The", "cd");
        public static readonly Flag CentralAfricanRepublic = new(41, "Central African Republic", "cf");
        public static readonly Flag Congo = new(42, "Congo", "cg");
        public static readonly Flag Switzerland = new(43, "Switzerland", "ch");
        public static readonly Flag CôteDIvoire = new(44, "Côte D'Ivoire", "ci");
        public static readonly Flag CookIslands = new(45, "Cook Islands", "ck");
        public static readonly Flag Chile = new(46, "Chile", "cl");
        public static readonly Flag Cameroon = new(47, "Cameroon", "cm");
        public static readonly Flag China = new(48, "China", "cn");
        public static readonly Flag Colombia = new(49, "Colombia", "co");
        public static readonly Flag CostaRica = new(50, "Costa Rica", "cr");
        public static readonly Flag Cuba = new(51, "Cuba", "cu");
        public static readonly Flag CaboVerde = new(52, "Cabo Verde", "cv");
        public static readonly Flag Curaçao = new(53, "Curaçao", "cw");
        public static readonly Flag ChristmasIsland = new(54, "Christmas Island", "cx");
        public static readonly Flag Cyprus = new(55, "Cyprus", "cy");
        public static readonly Flag Czechia = new(56, "Czechia", "cz");
        public static readonly Flag Germany = new(57, "Germany", "de");
        public static readonly Flag Djibouti = new(58, "Djibouti", "dj");
        public static readonly Flag Denmark = new(59, "Denmark", "dk");
        public static readonly Flag Dominica = new(60, "Dominica", "dm");
        public static readonly Flag DominicanRepublic = new(61, "Dominican Republic", "do");
        public static readonly Flag Algeria = new(62, "Algeria", "dz");
        public static readonly Flag Ecuador = new(63, "Ecuador", "ec");
        public static readonly Flag Estonia = new(64, "Estonia", "ee");
        public static readonly Flag Egypt = new(65, "Egypt", "eg");
        public static readonly Flag WesternSahara = new(66, "Western Sahara", "eh");
        public static readonly Flag Eritrea = new(67, "Eritrea", "er");
        public static readonly Flag Spain = new(68, "Spain", "es");
        public static readonly Flag Ethiopia = new(69, "Ethiopia", "et");
        public static readonly Flag Finland = new(70, "Finland", "fi");
        public static readonly Flag Fiji = new(71, "Fiji", "fj");
        public static readonly Flag FalklandIslands = new(72, "Falkland Islands", "fk");
        public static readonly Flag Micronesia = new(73, "Micronesia", "fm");
        public static readonly Flag FaroeIslands = new(74, "Faroe Islands", "fo");
        public static readonly Flag France = new(75, "France", "fr");
        public static readonly Flag Gabon = new(76, "Gabon", "ga");
        public static readonly Flag England = new(77, "England", "gb-eng");
        public static readonly Flag NorthernIreland = new(78, "Northern Ireland", "gb-nir");
        public static readonly Flag Scotland = new(79, "Scotland", "gb-sct");
        public static readonly Flag Wales = new(80, "Wales", "gb-wls");
        public static readonly Flag UnitedKingdom = new(81, "United Kingdom", "gb");
        public static readonly Flag Grenada = new(82, "Grenada", "gd");
        public static readonly Flag Georgia = new(83, "Georgia", "ge");
        public static readonly Flag FrenchGuiana = new(84, "French Guiana", "gf");
        public static readonly Flag Guernsey = new(85, "Guernsey", "gg");
        public static readonly Flag Ghana = new(86, "Ghana", "gh");
        public static readonly Flag Gibraltar = new(87, "Gibraltar", "gi");
        public static readonly Flag Greenland = new(88, "Greenland", "gl");
        public static readonly Flag Gambia = new(89, "Gambia", "gm");
        public static readonly Flag Guinea = new(90, "Guinea", "gn");
        public static readonly Flag Guadeloupe = new(91, "Guadeloupe", "gp");
        public static readonly Flag EquatorialGuinea = new(92, "Equatorial Guinea", "gq");
        public static readonly Flag Greece = new(93, "Greece", "gr");
        public static readonly Flag SouthGeorgiaAndTheSouthSandwichIslands = new(94, "South Georgia And The South Sandwich Islands", "gs");
        public static readonly Flag Guatemala = new(95, "Guatemala", "gt");
        public static readonly Flag Guam = new(96, "Guam", "gu");
        public static readonly Flag GuineaBissau = new(97, "Guinea-Bissau", "gw");
        public static readonly Flag Guyana = new(98, "Guyana", "gy");
        public static readonly Flag HongKong = new(99, "Hong Kong", "hk");
        public static readonly Flag HeardIslandAndMcdonaldIslands = new(100, "Heard Island And Mcdonald Islands", "hm");
        public static readonly Flag Honduras = new(101, "Honduras", "hn");
        public static readonly Flag Croatia = new(102, "Croatia", "hr");
        public static readonly Flag Haiti = new(103, "Haiti", "ht");
        public static readonly Flag Hungary = new(104, "Hungary", "hu");
        public static readonly Flag Indonesia = new(105, "Indonesia", "id");
        public static readonly Flag Ireland = new(106, "Ireland", "ie");
        public static readonly Flag Israel = new(107, "Israel", "il");
        public static readonly Flag IsleOfMan = new(108, "Isle Of Man", "im");
        public static readonly Flag India = new(109, "India", "in");
        public static readonly Flag BritishIndianOceanTerritory = new(110, "British Indian Ocean Territory", "io");
        public static readonly Flag Iraq = new(111, "Iraq", "iq");
        public static readonly Flag Iran = new(112, "Iran", "ir");
        public static readonly Flag Iceland = new(113, "Iceland", "is");
        public static readonly Flag Italy = new(114, "Italy", "it");
        public static readonly Flag Jersey = new(115, "Jersey", "je");
        public static readonly Flag Jamaica = new(116, "Jamaica", "jm");
        public static readonly Flag Jordan = new(117, "Jordan", "jo");
        public static readonly Flag Japan = new(118, "Japan", "jp");
        public static readonly Flag Kenya = new(119, "Kenya", "ke");
        public static readonly Flag Kyrgyzstan = new(120, "Kyrgyzstan", "kg");
        public static readonly Flag Cambodia = new(121, "Cambodia", "kh");
        public static readonly Flag Kiribati = new(122, "Kiribati", "ki");
        public static readonly Flag Comoros = new(123, "Comoros", "km");
        public static readonly Flag SaintKittsAndNevis = new(124, "Saint Kitts And Nevis", "kn");
        public static readonly Flag KoreaDemocraticPeoplesRepublicOf = new(125, "Korea, Democratic People's Republic Of", "kp");
        public static readonly Flag KoreaRepublicOf = new(126, "Korea, Republic Of", "kr");
        public static readonly Flag Kuwait = new(127, "Kuwait", "kw");
        public static readonly Flag CaymanIslands = new(128, "Cayman Islands", "ky");
        public static readonly Flag Kazakhstan = new(129, "Kazakhstan", "kz");
        public static readonly Flag LaoPeoplesDemocraticRepublic = new(130, "Lao People's Democratic Republic", "la");
        public static readonly Flag Lebanon = new(131, "Lebanon", "lb");
        public static readonly Flag SaintLucia = new(132, "Saint Lucia", "lc");
        public static readonly Flag Liechtenstein = new(133, "Liechtenstein", "li");
        public static readonly Flag SriLanka = new(134, "Sri Lanka", "lk");
        public static readonly Flag Liberia = new(135, "Liberia", "lr");
        public static readonly Flag Lesotho = new(136, "Lesotho", "ls");
        public static readonly Flag Lithuania = new(137, "Lithuania", "lt");
        public static readonly Flag Luxembourg = new(138, "Luxembourg", "lu");
        public static readonly Flag Latvia = new(139, "Latvia", "lv");
        public static readonly Flag Libya = new(140, "Libya", "ly");
        public static readonly Flag Morocco = new(141, "Morocco", "ma");
        public static readonly Flag Monaco = new(142, "Monaco", "mc");
        public static readonly Flag MoldovaRepublicOf = new(143, "Moldova, Republic Of", "md");
        public static readonly Flag Montenegro = new(144, "Montenegro", "me");
        public static readonly Flag SaintMartinFrenchPart = new(145, "Saint Martin (French Part)", "mf");
        public static readonly Flag Madagascar = new(146, "Madagascar", "mg");
        public static readonly Flag MarshallIslands = new(147, "Marshall Islands", "mh");
        public static readonly Flag NorthMacedonia = new(148, "North Macedonia", "mk");
        public static readonly Flag Mali = new(149, "Mali", "ml");
        public static readonly Flag Myanmar = new(150, "Myanmar", "mm");
        public static readonly Flag Mongolia = new(151, "Mongolia", "mn");
        public static readonly Flag Macao = new(152, "Macao", "mo");
        public static readonly Flag NorthernMarianaIslands = new(153, "Northern Mariana Islands", "mp");
        public static readonly Flag Martinique = new(154, "Martinique", "mq");
        public static readonly Flag Mauritania = new(155, "Mauritania", "mr");
        public static readonly Flag Montserrat = new(156, "Montserrat", "ms");
        public static readonly Flag Malta = new(157, "Malta", "mt");
        public static readonly Flag Mauritius = new(158, "Mauritius", "mu");
        public static readonly Flag Maldives = new(159, "Maldives", "mv");
        public static readonly Flag Malawi = new(160, "Malawi", "mw");
        public static readonly Flag Mexico = new(161, "Mexico", "mx");
        public static readonly Flag Malaysia = new(162, "Malaysia", "my");
        public static readonly Flag Mozambique = new(163, "Mozambique", "mz");
        public static readonly Flag Namibia = new(164, "Namibia", "na");
        public static readonly Flag NewCaledonia = new(165, "New Caledonia", "nc");
        public static readonly Flag Niger = new(166, "Niger", "ne");
        public static readonly Flag NorfolkIsland = new(167, "Norfolk Island", "nf");
        public static readonly Flag Nigeria = new(168, "Nigeria", "ng");
        public static readonly Flag Nicaragua = new(169, "Nicaragua", "ni");
        public static readonly Flag Netherlands = new(170, "Netherlands", "nl");
        public static readonly Flag Norway = new(171, "Norway", "no");
        public static readonly Flag Nepal = new(172, "Nepal", "np");
        public static readonly Flag Nauru = new(173, "Nauru", "nr");
        public static readonly Flag Niue = new(174, "Niue", "nu");
        public static readonly Flag NewZealand = new(175, "New Zealand", "nz");
        public static readonly Flag Oman = new(176, "Oman", "om");
        public static readonly Flag Panama = new(177, "Panama", "pa");
        public static readonly Flag Peru = new(178, "Peru", "pe");
        public static readonly Flag FrenchPolynesia = new(179, "French Polynesia", "pf");
        public static readonly Flag PapuaNewGuinea = new(180, "Papua New Guinea", "pg");
        public static readonly Flag Philippines = new(181, "Philippines", "ph");
        public static readonly Flag Pakistan = new(182, "Pakistan", "pk");
        public static readonly Flag Poland = new(183, "Poland", "pl");
        public static readonly Flag SaintPierreAndMiquelon = new(184, "Saint Pierre And Miquelon", "pm");
        public static readonly Flag Pitcairn = new(185, "Pitcairn", "pn");
        public static readonly Flag PuertoRico = new(186, "Puerto Rico", "pr");
        public static readonly Flag PalestineStateOf = new(187, "Palestine, State Of", "ps");
        public static readonly Flag Portugal = new(188, "Portugal", "pt");
        public static readonly Flag Palau = new(189, "Palau", "pw");
        public static readonly Flag Paraguay = new(190, "Paraguay", "py");
        public static readonly Flag Qatar = new(191, "Qatar", "qa");
        public static readonly Flag Réunion = new(192, "Réunion", "re");
        public static readonly Flag Romania = new(193, "Romania", "ro");
        public static readonly Flag Serbia = new(194, "Serbia", "rs");
        public static readonly Flag RussianFederation = new(195, "Russian Federation", "ru");
        public static readonly Flag Rwanda = new(196, "Rwanda", "rw");
        public static readonly Flag SaudiArabia = new(197, "Saudi Arabia", "sa");
        public static readonly Flag SolomonIslands = new(198, "Solomon Islands", "sb");
        public static readonly Flag Seychelles = new(199, "Seychelles", "sc");
        public static readonly Flag Sudan = new(200, "Sudan", "sd");
        public static readonly Flag Sweden = new(201, "Sweden", "se");
        public static readonly Flag Singapore = new(202, "Singapore", "sg");
        public static readonly Flag SaintHelenaAscensionAndTristanDaCunha = new(203, "Saint Helena, Ascension And Tristan Da Cunha", "sh");
        public static readonly Flag Slovenia = new(204, "Slovenia", "si");
        public static readonly Flag SvalbardAndJanMayen = new(205, "Svalbard And Jan Mayen", "sj");
        public static readonly Flag Slovakia = new(206, "Slovakia", "sk");
        public static readonly Flag SierraLeone = new(207, "Sierra Leone", "sl");
        public static readonly Flag SanMarino = new(208, "San Marino", "sm");
        public static readonly Flag Senegal = new(209, "Senegal", "sn");
        public static readonly Flag Somalia = new(210, "Somalia", "so");
        public static readonly Flag Suriname = new(211, "Suriname", "sr");
        public static readonly Flag SouthSudan = new(212, "South Sudan", "ss");
        public static readonly Flag SaoTomeAndPrincipe = new(213, "Sao Tome And Principe", "st");
        public static readonly Flag ElSalvador = new(214, "El Salvador", "sv");
        public static readonly Flag SintMaartenDutchPart = new(215, "Sint Maarten (Dutch Part)", "sx");
        public static readonly Flag SyrianArabRepublic = new(216, "Syrian Arab Republic", "sy");
        public static readonly Flag Eswatini = new(217, "Eswatini", "sz");
        public static readonly Flag TurksAndCaicosIslands = new(218, "Turks And Caicos Islands", "tc");
        public static readonly Flag Chad = new(219, "Chad", "td");
        public static readonly Flag FrenchSouthernTerritories = new(220, "French Southern Territories", "tf");
        public static readonly Flag Togo = new(221, "Togo", "tg");
        public static readonly Flag Thailand = new(222, "Thailand", "th");
        public static readonly Flag Tajikistan = new(223, "Tajikistan", "tj");
        public static readonly Flag Tokelau = new(224, "Tokelau", "tk");
        public static readonly Flag TimorLeste = new(225, "Timor-Leste", "tl");
        public static readonly Flag Turkmenistan = new(226, "Turkmenistan", "tm");
        public static readonly Flag Tunisia = new(227, "Tunisia", "tn");
        public static readonly Flag Tonga = new(228, "Tonga", "to");
        public static readonly Flag Turkey = new(229, "Turkey", "tr");
        public static readonly Flag TrinidadAndTobago = new(230, "Trinidad And Tobago", "tt");
        public static readonly Flag Tuvalu = new(231, "Tuvalu", "tv");
        public static readonly Flag Taiwan = new(232, "Taiwan", "tw");
        public static readonly Flag TanzaniaUnitedRepublicOf = new(233, "Tanzania, United Republic Of", "tz");
        public static readonly Flag Ukraine = new(234, "Ukraine", "ua");
        public static readonly Flag Uganda = new(235, "Uganda", "ug");
        public static readonly Flag UnitedStatesMinorOutlyingIslands = new(236, "United States Minor Outlying Islands", "um");
        public static readonly Flag UnitedStatesOfAmerica = new(237, "United States Of America", "us");
        public static readonly Flag Uruguay = new(238, "Uruguay", "uy");
        public static readonly Flag Uzbekistan = new(239, "Uzbekistan", "uz");
        public static readonly Flag HolySee = new(240, "Holy See", "va");
        public static readonly Flag SaintVincentAndTheGrenadines = new(241, "Saint Vincent And The Grenadines", "vc");
        public static readonly Flag VenezuelaBolivarianRepublicOf = new(242, "Venezuela (Bolivarian Republic Of)", "ve");
        public static readonly Flag VirginIslandsBritish = new(243, "Virgin Islands (British)", "vg");
        public static readonly Flag VirginIslandsUS = new(244, "Virgin Islands (U.S.)", "vi");
        public static readonly Flag VietNam = new(245, "Viet Nam", "vn");
        public static readonly Flag Vanuatu = new(246, "Vanuatu", "vu");
        public static readonly Flag WallisAndFutuna = new(247, "Wallis And Futuna", "wf");
        public static readonly Flag Samoa = new(248, "Samoa", "ws");
        public static readonly Flag Kosovo = new(249, "Kosovo", "xk");
        public static readonly Flag Yemen = new(250, "Yemen", "ye");
        public static readonly Flag Mayotte = new(251, "Mayotte", "yt");
        public static readonly Flag SouthAfrica = new(252, "South Africa", "za");
        public static readonly Flag Zambia = new(253, "Zambia", "zm");
        public static readonly Flag Zimbabwe = new(254, "Zimbabwe", "zw");

        private Flag(int fixedId, string name, string filename) {
            Id = GenerateId;
            FixedId = fixedId;
            Name = name;
            Filename = filename;
        }

        private static int GenerateId => _id++;
        public int FixedId { get; }
        public string Filename { get; }

        public int Id { get; }
        public string Name { get; }

        public static Flag FromString(string nameString) {
            return FdEnum.FromString(List(), nameString);
        }

        public static Flag FromId(int id) {
            return FdEnum.FromId(List(), id);
        }

        // Use this when storing state on leaderboards, these ids should never change!
        public static Flag FromFixedId(int fixedId) {
            var fdEnums = List();
            var flags = fdEnums as Flag[] ?? fdEnums.ToArray();

            try {
                return flags.Single(l => l.FixedId == fixedId);
            }
            catch {
                var firstElement = flags.First();
                Debug.Log($"Failed to parse enum fixed id {fixedId}, returning first element {firstElement.Name}");
                return firstElement;
            }
        }

        // Use when storing to user preferences, this is vaguely human-readable
        public static Flag FromFilename(string filename) {
            var fdEnums = List();
            var flags = fdEnums as Flag[] ?? fdEnums.ToArray();

            try {
                return flags.Single(l => l.Filename == filename);
            }
            catch {
                var firstElement = flags.First();
                Debug.Log($"Failed to parse enum with name {filename}, returning first element {firstElement.Name}");
                return firstElement;
            }
        }

        public static IEnumerable<Flag> List() {
            return new[] {
                None,
                Afghanistan,
                ÅlandIslands,
                Albania,
                Algeria,
                AmericanSamoa,
                Andorra,
                Angola,
                Anguilla,
                Antarctica,
                AntiguaAndBarbuda,
                Argentina,
                Armenia,
                Aruba,
                Australia,
                Austria,
                Azerbaijan,
                Bahamas,
                Bahrain,
                Bangladesh,
                Barbados,
                Belarus,
                Belgium,
                Belize,
                Benin,
                Bermuda,
                Bhutan,
                Bolivia,
                BonaireSintEustatiusAndSaba,
                BosniaAndHerzegovina,
                Botswana,
                BouvetIsland,
                Brazil,
                BritishIndianOceanTerritory,
                BruneiDarussalam,
                Bulgaria,
                BurkinaFaso,
                Burundi,
                CaboVerde,
                Cambodia,
                Cameroon,
                Canada,
                CaymanIslands,
                CentralAfricanRepublic,
                Chad,
                Chile,
                China,
                ChristmasIsland,
                CocosKeelingIslands,
                Colombia,
                Comoros,
                Congo,
                CongoDemocraticRepublicOfThe,
                CookIslands,
                CostaRica,
                CôteDIvoire,
                Croatia,
                Cuba,
                Curaçao,
                Cyprus,
                Czechia,
                Denmark,
                Djibouti,
                Dominica,
                DominicanRepublic,
                Ecuador,
                Egypt,
                ElSalvador,
                England,
                EquatorialGuinea,
                Eritrea,
                Estonia,
                Eswatini,
                Ethiopia,
                FalklandIslands,
                FaroeIslands,
                Fiji,
                Finland,
                France,
                FrenchGuiana,
                FrenchPolynesia,
                FrenchSouthernTerritories,
                Gabon,
                Gambia,
                Georgia,
                Germany,
                Ghana,
                Gibraltar,
                Greece,
                Greenland,
                Grenada,
                Guadeloupe,
                Guam,
                Guatemala,
                Guernsey,
                Guinea,
                GuineaBissau,
                Guyana,
                Haiti,
                HeardIslandAndMcdonaldIslands,
                HolySee,
                Honduras,
                HongKong,
                Hungary,
                Iceland,
                India,
                Indonesia,
                Iran,
                Iraq,
                Ireland,
                IsleOfMan,
                Israel,
                Italy,
                Jamaica,
                Japan,
                Jersey,
                Jordan,
                Kazakhstan,
                Kenya,
                Kiribati,
                KoreaDemocraticPeoplesRepublicOf,
                KoreaRepublicOf,
                Kosovo,
                Kuwait,
                Kyrgyzstan,
                LaoPeoplesDemocraticRepublic,
                Latvia,
                Lebanon,
                Lesotho,
                Liberia,
                Libya,
                Liechtenstein,
                Lithuania,
                Luxembourg,
                Macao,
                Madagascar,
                Malawi,
                Malaysia,
                Maldives,
                Mali,
                Malta,
                MarshallIslands,
                Martinique,
                Mauritania,
                Mauritius,
                Mayotte,
                Mexico,
                Micronesia,
                MoldovaRepublicOf,
                Monaco,
                Mongolia,
                Montenegro,
                Montserrat,
                Morocco,
                Mozambique,
                Myanmar,
                Namibia,
                Nauru,
                Nepal,
                Netherlands,
                NewCaledonia,
                NewZealand,
                Nicaragua,
                Niger,
                Nigeria,
                Niue,
                NorfolkIsland,
                NorthernIreland,
                NorthernMarianaIslands,
                NorthMacedonia,
                Norway,
                Oman,
                Pakistan,
                Palau,
                PalestineStateOf,
                Panama,
                PapuaNewGuinea,
                Paraguay,
                Peru,
                Philippines,
                Pitcairn,
                Poland,
                Portugal,
                PuertoRico,
                Qatar,
                Réunion,
                Romania,
                RussianFederation,
                Rwanda,
                SaintBarthélemy,
                SaintHelenaAscensionAndTristanDaCunha,
                SaintKittsAndNevis,
                SaintLucia,
                SaintMartinFrenchPart,
                SaintPierreAndMiquelon,
                SaintVincentAndTheGrenadines,
                Samoa,
                SanMarino,
                SaoTomeAndPrincipe,
                SaudiArabia,
                Scotland,
                Senegal,
                Serbia,
                Seychelles,
                SierraLeone,
                Singapore,
                SintMaartenDutchPart,
                Slovakia,
                Slovenia,
                SolomonIslands,
                Somalia,
                SouthAfrica,
                SouthGeorgiaAndTheSouthSandwichIslands,
                SouthSudan,
                Spain,
                SriLanka,
                Sudan,
                Suriname,
                SvalbardAndJanMayen,
                Sweden,
                Switzerland,
                SyrianArabRepublic,
                Taiwan,
                Tajikistan,
                TanzaniaUnitedRepublicOf,
                Thailand,
                TimorLeste,
                Togo,
                Tokelau,
                Tonga,
                TrinidadAndTobago,
                Tunisia,
                Turkey,
                Turkmenistan,
                TurksAndCaicosIslands,
                Tuvalu,
                Uganda,
                Ukraine,
                UnitedArabEmirates,
                UnitedKingdom,
                UnitedStatesMinorOutlyingIslands,
                UnitedStatesOfAmerica,
                Uruguay,
                Uzbekistan,
                Vanuatu,
                VenezuelaBolivarianRepublicOf,
                VietNam,
                VirginIslandsBritish,
                VirginIslandsUS,
                Wales,
                WallisAndFutuna,
                WesternSahara,
                Yemen,
                Zambia,
                Zimbabwe
            };
        }
    }
}