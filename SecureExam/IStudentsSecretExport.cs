﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureExam
{
    interface IStudentsSecretExport
    {
        bool export(String filename);
    }
}
