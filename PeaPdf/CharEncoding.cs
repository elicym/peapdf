/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    /*
     * Fonts can support multiple encodings. We always use Unicode, since it goes well with Skia.
     * PDFs are not encoded with Unicode. Default is StdEncoding. Therefore we need to convert from code of current encoding to Unicode.
     * Exception is when we create the cmap, then we create it according to the current encoding, even though we mark it as Unicode.
     */
    class CharEncoding
    {

        static List<Char> chars = new List<Char>
        {
            new Char('A',"A",34,65,65,65,65,65),
            new Char('Æ',"AE",138,225,174,198,198,198),
            new Char('Á',"Aacute",171,null,231,193,193,193),
            new Char('Â',"Acircumflex",172,null,229,194,194,194),
            new Char('Ä',"Adieresis",173,null,128,196,196,196),
            new Char('À',"Agrave",174,null,203,192,192,192),
            new Char('Å',"Aring",175,null,129,197,197,197),
            new Char('Ã',"Atilde",176,null,204,195,195,195),
            new Char('B',"B",35,66,66,66,66,66),
            new Char('C',"C",36,67,67,67,67,67),
            new Char('Ç',"Ccedilla",177,null,130,199,199,199),
            new Char('D',"D",37,68,68,68,68,68),
            new Char('E',"E",38,69,69,69,69,69),
            new Char('É',"Eacute",178,null,131,201,201,201),
            new Char('Ê',"Ecircumflex",179,null,230,202,202,202),
            new Char('Ë',"Edieresis",180,null,232,203,203,203),
            new Char('È',"Egrave",181,null,233,200,200,200),
            new Char('Ð',"Eth",154,null,null,208,208,208),
            new Char('€',"Euro",-1,null,null,128,160,8364),
            new Char('F',"F",39,70,70,70,70,70),
            new Char('G',"G",40,71,71,71,71,71),
            new Char('H',"H",41,72,72,72,72,72),
            new Char('I',"I",42,73,73,73,73,73),
            new Char('Í',"Iacute",182,null,234,205,205,205),
            new Char('Î',"Icircumflex",183,null,235,206,206,206),
            new Char('Ï',"Idieresis",184,null,236,207,207,207),
            new Char('Ì',"Igrave",185,null,237,204,204,204),
            new Char('J',"J",43,74,74,74,74,74),
            new Char('K',"K",44,75,75,75,75,75),
            new Char('L',"L",45,76,76,76,76,76),
            new Char('Ł',"Lslash",140,232,null,null,149,321),
            new Char('M',"M",46,77,77,77,77,77),
            new Char('N',"N",47,78,78,78,78,78),
            new Char('Ñ',"Ntilde",186,null,132,209,209,209),
            new Char('O',"O",48,79,79,79,79,79),
            new Char('O',"EOE",142,234,206,140,150,338),
            new Char('Ó',"Oacute",187,null,238,211,211,211),
            new Char('Ô',"Ocircumflex",188,null,239,212,212,212),
            new Char('Ö',"Odieresis",189,null,133,214,214,214),
            new Char('Ò',"Ograve",190,null,241,210,210,210),
            new Char('Ø',"Oslash",141,233,175,216,216,216),
            new Char('Õ',"Otilde",191,null,205,213,213,213),
            new Char('P',"P",49,80,80,80,80,80),
            new Char('Q',"Q",50,81,81,81,81,81),
            new Char('R',"R",51,82,82,82,82,82),
            new Char('S',"S",52,83,83,83,83,83),
            new Char('Š',"Scaron",192,null,null,138,151,352),
            new Char('T',"T",53,84,84,84,84,84),
            new Char('Þ',"Thorn",157,null,null,222,222,222),
            new Char('U',"U",54,85,85,85,85,85),
            new Char('Ú',"Uacute",193,null,242,218,218,218),
            new Char('Û',"Ucircumflex",194,null,243,219,219,219),
            new Char('Ü',"Udieresis",195,null,134,220,220,220),
            new Char('Ù',"Ugrave",196,null,244,217,217,217),
            new Char('V',"V",55,86,86,86,86,86),
            new Char('W',"W",56,87,87,87,87,87),
            new Char('X',"X",57,88,88,88,88,88),
            new Char('Y',"Y",58,89,89,89,89,89),
            new Char('Ý',"Yacute",197,null,null,221,221,221),
            new Char('Ÿ',"Ydieresis",198,null,217,159,152,376),
            new Char('Z',"Z",59,90,90,90,90,90),
            new Char('Ž',"Zcaron",199,null,null,142,153,381),
            new Char('a',"a",66,97,97,97,97,97),
            new Char('á',"aacute",200,null,135,225,225,225),
            new Char('â',"acircumflex",201,null,137,226,226,226),
            new Char('´',"acute",125,194,171,180,180,180),
            new Char('ä',"adieresis",202,null,138,228,228,228),
            new Char('æ',"ae",144,241,190,230,230,230),
            new Char('à',"agrave",203,null,136,224,224,224),
            new Char('&',"ampersand",7,38,38,38,38,38),
            new Char('å',"aring",204,null,140,229,229,229),
            new Char('^',"asciicircum",63,94,94,94,94,94),
            new Char('~',"asciitilde",95,126,126,126,126,126),
            new Char('*',"asterisk",11,42,42,42,42,42),
            new Char('@',"at",33,64,64,64,64,64),
            new Char('ã',"atilde",205,null,139,227,227,227),
            new Char('b',"b",67,98,98,98,98,98),
            new Char('\\',"backslash",61,92,92,92,92,92),
            new Char('|',"bar",93,124,124,124,124,124),
            new Char('{',"braceleft",92,123,123,123,123,123),
            new Char('}',"braceright",94,125,125,125,125,125),
            new Char('[',"bracketleft",60,91,91,91,91,91),
            new Char(']',"bracketright",62,93,93,93,93,93),
            new Char('˘',"breve",129,198,249,null,24,728),
            new Char('¦',"brokenbar",160,null,null,166,166,166),
            new Char('•',"bullet",116,183,165,149,128,8226),
            new Char('c',"c",68,99,99,99,99,99),
            new Char('ˇ',"caron",136,207,255,null,25,711),
            new Char('ç',"ccedilla",206,null,141,231,231,231),
            new Char('¸',"cedilla",133,203,252,184,184,184),
            new Char('¢',"cent",97,162,162,162,162,162),
            new Char('ˆ',"circumflex",126,195,246,136,26,710),
            new Char(':',"colon",27,58,58,58,58,58),
            new Char(',',"comma",13,44,44,44,44,44),
            new Char('©',"copyright",170,null,169,169,169,169),
            new Char('¤',"currency",103,168,219,164,164,164),
            new Char('d',"d",69,100,100,100,100,100),
            new Char('†',"dagger",112,178,160,134,129,8224),
            new Char('‡',"daggerdbl",113,179,224,135,130,8225),
            new Char('°',"degree",161,null,161,176,176,176),
            new Char('¨',"dieresis",131,200,172,168,168,168),
            new Char('÷',"divide",159,null,214,247,247,247),
            new Char('$',"dollar",5,36,36,36,36,36),
            new Char('˙',"dotaccent",130,199,250,null,27,729),
            new Char('ı',"dotlessi",145,245,245,null,154,305),
            new Char('e',"e",70,101,101,101,101,101),
            new Char('é',"eacute",207,null,142,233,233,233),
            new Char('ê',"ecircumflex",208,null,144,234,234,234),
            new Char('ë',"edieresis",209,null,145,235,235,235),
            new Char('è',"egrave",210,null,143,232,232,232),
            new Char('8',"eight",25,56,56,56,56,56),
            new Char('…',"ellipsis",121,188,201,133,131,8230),
            new Char('—',"emdash",137,208,209,151,132,8212),
            new Char('–',"endash",111,177,208,150,133,8211),
            new Char('=',"equal",30,61,61,61,61,61),
            new Char('ð',"eth",167,null,null,240,240,240),
            new Char('!',"exclam",2,33,33,33,33,33),
            new Char('¡',"exclamdown",96,161,193,161,161,161),
            new Char('f',"f",71,102,102,102,102,102),
            new Char('f',"ifi",109,174,222,null,147,64257),
            new Char('5',"five",22,53,53,53,53,53),
            new Char('f',"lfl",110,175,223,null,148,64258),
            new Char('ƒ',"florin",101,166,196,131,134,402),
            new Char('4',"four",21,52,52,52,52,52),
            new Char('⁄',"fraction",99,164,218,null,135,8260),
            new Char('g',"g",72,103,103,103,103,103),
            new Char('ß',"germandbls",149,251,167,223,223,223),
            new Char('`',"grave",124,193,96,96,96,96),
            new Char('>',"greater",31,62,62,62,62,62),
            new Char('«',"guillemotleft",106,171,199,171,171,171),
            new Char('»',"guillemotright",120,187,200,187,187,187),
            new Char('‹',"guilsinglleft",107,172,220,139,136,8249),
            new Char('›',"guilsinglright",108,173,221,155,137,8250),
            new Char('h',"h",73,104,104,104,104,104),
            new Char('˝',"hungarumlaut",134,205,253,null,28,733),
            new Char('-',"hyphen",14,45,45,45,45,45),
            new Char('i',"i",74,105,105,105,105,105),
            new Char('í',"iacute",211,null,146,237,237,237),
            new Char('î',"icircumflex",212,null,148,238,238,238),
            new Char('ï',"idieresis",213,null,149,239,239,239),
            new Char('ì',"igrave",214,null,147,236,236,236),
            new Char('j',"j",75,106,106,106,106,106),
            new Char('k',"k",76,107,107,107,107,107),
            new Char('l',"l",77,108,108,108,108,108),
            new Char('<',"less",29,60,60,60,60,60),
            new Char('¬',"logicalnot",151,null,194,172,172,172),
            new Char('ł',"lslash",146,248,null,null,155,322),
            new Char('m',"m",78,109,109,109,109,109),
            new Char('¯',"macron",128,197,248,175,175,175),
            new Char('−',"minus",166,null,null,null,138,8722),
            new Char('μ',"mu",152,null,181,181,181,181),
            new Char('×',"multiply",168,null,null,215,215,215),
            new Char('n',"n",79,110,110,110,110,110),
            new Char('9',"nine",26,57,57,57,57,57),
            new Char('ñ',"ntilde",215,null,150,241,241,241),
            new Char('#',"numbersign",4,35,35,35,35,35),
            new Char('o',"o",80,111,111,111,111,111),
            new Char('ó',"oacute",216,null,151,243,243,243),
            new Char('ô',"ocircumflex",217,null,153,244,244,244),
            new Char('ö',"odieresis",218,null,154,246,246,246),
            new Char('o',"eoe",-5,250,207,156,156,339),
            new Char('˛',"ogonek",135,206,254,null,29,731),
            new Char('ò',"ograve",219,null,152,242,242,242),
            new Char('1',"one",18,49,49,49,49,49),
            new Char('½',"onehalf",155,null,null,189,189,189),
            new Char('¼',"onequarter",158,null,null,188,188,188),
            new Char('¹',"onesuperior",150,null,null,185,185,185),
            new Char('ª',"ordfeminine",139,227,187,170,170,170),
            new Char('º',"ordmasculine",143,235,188,186,186,186),
            new Char('ø',"oslash",147,249,191,248,248,248),
            new Char('õ',"otilde",220,null,155,245,245,245),
            new Char('p',"p",81,112,112,112,112,112),
            new Char('¶',"paragraph",115,182,166,182,182,182),
            new Char('(',"parenleft",9,40,40,40,40,40),
            new Char(')',"parenright",10,41,41,41,41,41),
            new Char('%',"percent",6,37,37,37,37,37),
            new Char('.',"period",15,46,46,46,46,46),
            new Char('·',"periodcentered",114,180,225,183,183,183),
            new Char('‰',"perthousand",122,189,228,137,139,8240),
            new Char('+',"plus",12,43,43,43,43,43),
            new Char('±',"plusminus",156,null,177,177,177,177),
            new Char('q',"q",82,113,113,113,113,113),
            new Char('?',"question",32,63,63,63,63,63),
            new Char('¿',"questiondown",123,191,192,191,191,191),
            new Char('"',"quotedbl",3,34,34,34,34,34),
            new Char('„',"quotedblbase",118,185,227,132,140,8222),
            new Char('“',"quotedblleft",105,170,210,147,141,8220),
            new Char('”',"quotedblright",119,186,211,148,142,8221),
            new Char('‘',"quoteleft",65,96,212,145,143,8216),
            new Char('’',"quoteright",8,39,213,146,144,8217),
            new Char('‚',"quotesinglbase",117,184,226,130,145,8218),
            new Char('\'',"quotesingle",104,169,39,39,39,39),
            new Char('r',"r",83,114,114,114,114,114),
            new Char('®',"registered",165,null,168,174,174,174),
            new Char('˚',"ring",132,202,251,null,30,730),
            new Char('s',"s",84,115,115,115,115,115),
            new Char('š',"scaron",221,null,null,154,157,353),
            new Char('§',"section",102,167,164,167,167,167),
            new Char(';',"semicolon",28,59,59,59,59,59),
            new Char('7',"seven",24,55,55,55,55,55),
            new Char('6',"six",23,54,54,54,54,54),
            new Char('/',"slash",16,47,47,47,47,47),
            new Char('q',"space",1,32,32,32,32,32),
            new Char('£',"sterling",98,163,163,163,163,163),
            new Char('t',"t",85,116,116,116,116,116),
            new Char('þ',"thorn",162,null,null,254,254,254),
            new Char('3',"three",20,51,51,51,51,51),
            new Char('¾',"threequarters",163,null,null,190,190,190),
            new Char('³',"threesuperior",169,null,null,179,179,179),
            new Char('˜',"tilde",127,196,247,152,31,732),
            new Char('™',"trademark",153,null,170,153,146,8482),
            new Char('2',"two",19,50,50,50,50,50),
            new Char('²',"twosuperior",164,null,null,178,178,178),
            new Char('u',"u",86,117,117,117,117,117),
            new Char('ú',"uacute",222,null,156,250,250,250),
            new Char('û',"ucircumflex",223,null,158,251,251,251),
            new Char('ü',"udieresis",224,null,159,252,252,252),
            new Char('ù',"ugrave",225,null,157,249,249,249),
            new Char('_',"underscore",64,95,95,95,95,95),
            new Char('v',"v",87,118,118,118,118,118),
            new Char('w',"w",88,119,119,119,119,119),
            new Char('x',"x",89,120,120,120,120,120),
            new Char('y',"y",90,121,121,121,121,121),
            new Char('ý',"yacute",226,null,null,253,253,253),
            new Char('ÿ',"ydieresis",227,null,216,255,255,255),
            new Char('¥',"yen",100,165,180,165,165,165),
            new Char('z',"z",91,122,122,122,122,122),
            new Char('ž',"zcaron",228,null,null,158,158,382),
            new Char('0',"zero",17,48,48,48,48,48)
        };
        static Dictionary<int, Char> charsBySID = chars.ToDictionary(x => x.SID);
        public static Dictionary<string, int> SIDByName = chars.ToDictionary(x => x.Name, x => x.SID);
        public static Dictionary<string, ushort> UnicodeByName = chars.ToDictionary(x => x.Name, x => x.Unicode);

        public static CharEncoding WinAnsiEncoding = new CharEncoding(x => x.WIN != null, x => x.WIN.Value),
            StdEncoding = new CharEncoding(x => x.STD != null, x => x.STD.Value),
            MacEncoding = new CharEncoding(x => x.MAC != null, x => x.MAC.Value);

        public static CharEncoding FromName(string name)
        {
            switch (name)
            {
                case "WinAnsiEncoding": return WinAnsiEncoding;
                case "MacRomanEncoding": return MacEncoding;
                case "Identity-H": return null;
                default: throw new NotSupportedException();
            }
        }

        class Char
        {
            public char C;
            public readonly string Name;
            public readonly int SID;
            public readonly byte? STD, MAC, WIN;
            public readonly byte PDF;
            public readonly ushort Unicode;

            public Char(char c, string name, int sid, byte? std, byte? mac, byte? win, byte pdf, ushort unicode)
            {
                C = c;
                Name = name;
                SID = sid;
                STD = std; MAC = mac; WIN = win; PDF = pdf;
                Unicode = unicode;
            }
        }

        CharEncoding(Func<Char, bool> predicate, Func<Char, byte> selector)
        {
            var _chars = chars.Where(predicate);
            code2SIDs = _chars.ToDictionary(selector, x => x.SID);
            sid2Codes = _chars.ToDictionary(x => x.SID, selector);
            code2Unicodes = _chars.ToDictionary(selector, x => x.Unicode);
            name2Codes = _chars.ToDictionary(x => x.Name, selector);
            code2Names = _chars.ToDictionary(selector, x => x.Name);
        }

        Dictionary<byte, int> code2SIDs;
        Dictionary<int, byte> sid2Codes;
        Dictionary<byte, ushort> code2Unicodes;
        Dictionary<string, byte> name2Codes;
        Dictionary<byte, string> code2Names;

        public int? Code2SID(byte code)
        {
            if (code2SIDs.TryGetValue(code, out var v))
                return v;
            return null;
        }

        public int? SID2Code(int sid)
        {
            if (sid2Codes.TryGetValue(sid, out var v))
                return v;
            return null;
        }

        public int? Code2Unicode(byte code)
        {
            if (code2Unicodes.TryGetValue(code, out var v))
                return v;
            return null;
        }

        public int? Name2Code(string name)
        {
            if (name2Codes.TryGetValue(name, out var v))
                return v;
            return null;
        }

        public string Code2Name(byte code)
        {
            if (code2Names.TryGetValue(code, out var v))
                return v;
            return null;
        }

    }

}
