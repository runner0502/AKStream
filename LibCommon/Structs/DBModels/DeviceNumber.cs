using FreeSql.DataAnnotations;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "DeviceNumber")]
    public class DeviceNumber
    {
        [Column(IsPrimary = true, IsIdentity =true  )]
        public int id { get; set; }
        private string _dev;

        [Column(DbType = "varchar(45)")]
        public string dev { get => _dev; set => _dev = value; }

        public string num { get; set; }

        private string _num;

        [Column(DbType = "varchar(45)")]
        public string nev { get => _num; set => _num = value; }

        private string _name;

        [Column(DbType = "varchar(255)")]
        public string name { get => _name; set => _name = value; }

        private string _fatherid;

        [Column(DbType = "varchar(45)")]
        public string fatherid { get => _fatherid; set => _fatherid = value; }


        private string _logitude;

        [Column(DbType = "varchar(45)")]
        public string longitude { get => _logitude; set => _logitude = value; }

        private string _latitude;

        [Column(DbType = "varchar(45)")]
        public string latitude { get => _latitude; set => _latitude = value; }

        private string _domain;

        [Column(DbType = "varchar(45)")]
        public string domain { get => _domain; set => _domain = value; }

        private string _devMa;

        [Column(DbType = "varchar(45)")]
        public string devMa { get => _devMa; set => _devMa = value; }

        private string _place;

        [Column(DbType = "varchar(45)")]
        public string place { get => _place; set => _place = value; }

        private int _ptz_type;
        [Column(DbType = "int(11)")]
        public int ptz_type { get => _ptz_type; set => _ptz_type = value; }

        private int _period;
        [Column(DbType = "int(11)")]
        public int period { get => _period; set => _period = value; }

        private DateTime _modify_time;
        //[Column(DbType = "timestamp")]
        public DateTime modify_time { get => _modify_time; set => _modify_time = value; }

        public int status { get; set; }

    }
}
