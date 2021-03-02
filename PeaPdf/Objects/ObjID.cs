using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    readonly struct ObjID : IEquatable<ObjID>
    {
        public readonly int ObjNum, GenNum;

        public ObjID(int objNum, int genNum)
        {
            ObjNum = objNum;
            GenNum = genNum;
        }

        public override bool Equals(object obj) => obj is ObjID other ? Equals(other) : false;
        public bool Equals(ObjID other) => ObjNum == other.ObjNum && GenNum == other.GenNum;
        public override int GetHashCode() => (17 * 31 + ObjNum) * 31 + GenNum;
        public static bool operator ==(ObjID obj1, ObjID obj2) => obj1.Equals(obj2);
        public static bool operator !=(ObjID obj1, ObjID obj2) => !obj1.Equals(obj2);

    }
}
