﻿using QLicenseCore;
using System.ComponentModel;
using System.Xml.Serialization;
using System;
using XyCallLayer;
using LibCommon;

namespace AKStreamWeb
{

    public class LicenseNative : QLicenseCore.MyLicenseCommon
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

        public LicenseNative()
        {
            //Initialize app name for the license
            this.AppName = "281";
            this.Type = LicenseTypes.Single;
        }

        public override LicenseStatus DoExtraValidation(out string validationMsg)
        {
            LicenseStatus _licStatus = LicenseStatus.UNDEFINED;
            validationMsg = string.Empty;

            switch (this.Type)
            {
                case LicenseTypes.Single:
                    //For Single License, check whether UID is matched
                    SPhoneSDK.LicenseResult result = new SPhoneSDK.LicenseResult();
                    bool valid = SPhoneSDK.VerifyLicense(ref result);
                    MaxDeviceCount = result.maxDevice;
                    MaxPushNumber = result.maxPushMedia;
                    MaxRunCount = result.maxTranscode;
                    try
                    {
                        ExpireDateTime = DateTime.Parse(result.expireDate);
                    }
                    catch (Exception ex)
                    {
                        GCommon.Logger.Warn(ex.Message);
                    }

                    GCommon.Logger.Debug("license info " + result.ToJson().ToString());

                    if (valid)
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
                    _licStatus = LicenseStatus.INVALID;
                    break;
            }

            return _licStatus;
        }


    }
}
