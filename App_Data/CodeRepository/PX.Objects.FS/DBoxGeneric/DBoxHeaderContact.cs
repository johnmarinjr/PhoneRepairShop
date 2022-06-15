using PX.Data;
using PX.Objects.AP;
using PX.Objects.IN;
using System;
using PX.Objects.CR;

namespace PX.Objects.FS
{
    public class DBoxHeaderContact
    {
        #region Title
        public virtual string Title { get; set; }
        #endregion
        #region Attention
        public virtual string Attention { get; set; }
        #endregion
        #region FullName
        public virtual string FullName { get; set; }
        #endregion
        #region Email
        public virtual string Email { get; set; }
        #endregion
        #region Phone1
        public virtual string Phone1 { get; set; }
        #endregion
        #region Phone2
        public virtual string Phone2 { get; set; }
        #endregion
        #region Phone3
        public virtual string Phone3 { get; set; }
        #endregion
        #region Fax
        public virtual string Fax { get; set; }
        #endregion

        public static implicit operator DBoxHeaderContact(CRContact contact)
        {
            if (contact == null)
            {
                return null;
            }

            DBoxHeaderContact ret = new DBoxHeaderContact();

            ret.Title = contact.Title;
            ret.Attention = contact.Attention;
            ret.FullName = contact.FullName;
            ret.Email = contact.Email;
            ret.Phone1 = contact.Phone1;
            ret.Phone2 = contact.Phone2;
            ret.Phone3 = contact.Phone3;
            ret.Fax = contact.Fax;

            return ret;
        }
    }
}

