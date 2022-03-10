using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    static class Constants
    {
        public const string GETFILEURL = "{0}GetFile.ashx?functionName={1}&{2}";
        //public const string HEADERNAMESPACE = @"http://www.docstar.com/ns/api";
        public const string HEADERNAMESPACE = @"https://docstarapp01.main.agmcontainer.com/ns/api";
        public const string TOKENHEADER = "ds-token"; //Authentication Token
        public const string OPTIONSHEADER = "ds-options"; //Service Options (ServiceRequestOptions), Used to override errors on overridable exception types (ex:Deleting a document under records management)
        public const string SOURCEHEADER = "ds-source";
        //public const string SERVICE_SOURCE = "http://www.docstar.com/Eclipse/ClientService";
        public const string SERVICE_SOURCE = @"https://docstarapp01.main.agmcontainer.com/Eclipse/ClientService";
        public const int CHUNKSIZE = 0x80000; // 512 KByte, can be larger but have to be careful not to overload the server.


        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern int UuidCreateSequential(out Guid guid);
        /// <summary>
        /// NOTE: There is an API for this if the language you are using does not support this method.
        /// </summary>
        /// <returns>A SQL server sequential guid</returns>
        public static Guid NewSeq()
        {
            Guid guid;
            UuidCreateSequential(out guid);
            //Byte shuffling that SQLs NEWSEQUENTIALID performs:
            var s = guid.ToByteArray();
            var t = new byte[16];
            //Bytes in groups 0-3, 4-5, and 6-7 are reversed, See SQL sorting notes below
            t[3] = s[0];
            t[2] = s[1];
            t[1] = s[2];
            t[0] = s[3];

            t[5] = s[4];
            t[4] = s[5];

            t[7] = s[6];
            t[6] = s[7];

            //Bytes in groups 8-9, and 10-15 are in normal order
            t[8] = s[8];
            t[9] = s[9];

            t[10] = s[10];
            t[11] = s[11];
            t[12] = s[12];
            t[13] = s[13];
            t[14] = s[14];
            t[15] = s[15];
            /**********************A Note About SQL Guid Sorting*****************************************
            *   0..3 are evaluated in right to left order and are the less important, then              *
            *   4..5 are evaluated in right to left order, then                                         *
            *   6..7 are evaluated in right to left order, then                                         *
            *   8..9 are evaluated in left to right order, then                                         *
            *   10..15 are evaluated in left to right order and are the most important                  *
            ********************************************************************************************/
            return new Guid(t);
        }
    }
    public enum PageSize
    {
        A4 = 1,
        Letter = 3,
        Legal = 4,
        A5 = 5,
        UsLedger = 9,
        UsExecutive = 10,
        A3 = 11,
        UsStatement = 52,
        BusinessCard = 53
    }
    public enum CFTypeCode
    {
        Empty = 0,
        Object = 1,
        Boolean = 3,
        Int16 = 7,
        Int32 = 9,
        Int64 = 11,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18,
        Date = 1000,
        Guid = 1001
    }
}
