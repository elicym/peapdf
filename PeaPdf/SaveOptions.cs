/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    public class SaveOptions
    {
        public bool NoObjectStreams;
        public EncryptionSaveOptions Encryption;
        internal byte[] FileID;
    }

    public class EncryptionSaveOptions
    {
        public readonly string UserPwd;
        public readonly string OwnerPwd;
        public readonly bool UseRC4;
        public readonly UserAccessPermissions UserAccessPermissions;

        public EncryptionSaveOptions(string userPwd, UserAccessPermissions userAccessPermissions, string ownerPwd = null, bool useRC4 = false)
        {
            //An empty string is permitted. This will cause the PDF to be encrypted, but can be decrypted without a password.
            //Not sure what the point is, but it's used, such as the PDF specification document itself.
            if (userPwd == null) throw new ArgumentNullException(nameof(userPwd));
            CheckPwd(userPwd);
            UserAccessPermissions = userAccessPermissions;
            UserPwd = userPwd;
            if (ownerPwd != null)
                CheckPwd(ownerPwd);
            OwnerPwd = ownerPwd;
            UseRC4 = useRC4;
        }

        void CheckPwd(string pwd)
        {
            if (pwd.Any(x => x >= 128)) throw new Exception("Invalid characters in password.");
        }
    }

    [Flags]
    public enum UserAccessPermissions { Print = 0x4, Modify = 0x8, Copy = 0x10, Fields = 0x20, FillFields = 0x100, Assemble = 0x400, PrintAccurate = 0x800 }

}
