﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Thinkment.Data
{
    [Serializable]
    public class DataException : Exception
    {
        ErrorCodes errorcode = ErrorCodes.Success;

        public ErrorCodes Errorcode
        {
            get { return errorcode; }
            private set { errorcode = value; }
        }

        public DataException(ErrorCodes code)
            : base()
        {
            errorcode = code;
        }

        public DataException(string message, ErrorCodes code)
            : base(message)
        {
            errorcode = code;
        }

        public DataException(string msg)
            : base(msg)
        { 
        
        }
    }
}
