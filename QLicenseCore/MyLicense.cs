﻿using QLicenseCore;
using System;
using System.ComponentModel;
using System.Xml.Serialization;


namespace QLicenseCore
{
    public class MyLicense : QLicenseCore.LicenseEntity
    {
        [DisplayName("Enable Feature 01")]
        [Category("License Options")]        
        [XmlElement("EnableFeature01")]
        [ShowInLicenseInfo(true, "Enable Feature 01", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature01 { get; set; }

        [DisplayName("Enable Feature 02")]
        [Category("License Options")]        
        [XmlElement("EnableFeature02")]
        [ShowInLicenseInfo(true, "Enable Feature 02", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature02 { get; set; }


        [DisplayName("Enable Feature 03")]
        [Category("License Options")]        
        [XmlElement("EnableFeature03")]
        [ShowInLicenseInfo(true, "Enable Feature 03", ShowInLicenseInfoAttribute.FormatType.String)]
        public bool EnableFeature03 { get; set; }

        public MyLicense()
        {
            //Initialize app name for the license
            this.AppName = "281";
        }

        public override LicenseStatus DoExtraValidation(out string validationMsg)
        {
            LicenseStatus _licStatus = LicenseStatus.UNDEFINED;
            validationMsg = string.Empty;

            switch (this.Type)
            {
                case LicenseTypes.Single:
                    //For Single License, check whether UID is matched
                    if (this.UID == LicenseHandler.GenerateUID(this.AppName) &&
                        this.ExpireDateTime > DateTime.Now)
                    {
                        _licStatus = LicenseStatus.VALID;
                    }
                    else
                    {
                        validationMsg = "The license is NOT for this copy!";
                        _licStatus = LicenseStatus.INVALID;                    
                    }
                    break;
                case LicenseTypes.Volume:
                    //No UID checking for Volume License
                    _licStatus = LicenseStatus.VALID;
                    break;
                default:
                    validationMsg = "Invalid license";
                    _licStatus= LicenseStatus.INVALID;
                    break;
            }

            return _licStatus;
        }

        public int MaxDeviceCount { get; set; }
        public int MaxRunCount { get; set; }
        public int MaxPushNumber { get; set; }


        public DateTime ExpireDateTime { get; set; }

    }
}
