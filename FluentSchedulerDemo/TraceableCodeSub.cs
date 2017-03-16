using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSchedulerDemo
{
    public class TraceableCodeSub
    {
        public string Sid { get; set; }

        public string Mid { get; set; }

        public string OrderNo { get; set; }

        public int CodeType { get; set; }

        public string TraceableCode { get; set; }

        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 分区列，企业id
        /// </summary>
        public Guid OrgID { get; set; }
    }
}
