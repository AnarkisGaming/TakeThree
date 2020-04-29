/*
 * TakeThree, as originally included in After the Collapse
 * Copyright (C) 2016-2020 Anarkis Gaming. All rights reserved.
 *  
 * This file and all other files accompanying this distribution are
 * licensed to you under the Microsoft Reciprocal Source License. Please see
 * the LICENSE file for more details.
 * 
 * As a reminder: the software is licensed "as-is." You bear the risk of using
 * it. The contributors give no express warranties, guarantees or conditions.
 * You may have additional consumer rights under your local laws which this
 * license cannot change. To the extent permitted under your local laws, the
 * contributors exclude the implied warranties of merchantability, fitness for
 * a particular purpose and non-infringement.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TakeThree
{
    public class ModConfiguration
    {
        public string name;
        public string author;
        public ulong workshopId = 0;
        public double version = 0.0;
        public double worksVersion = 0.4;
        public string entryPoint = null;
        public string description;
    }

    public enum FilePrivacy
    {
        PUBLIC,
        FRIENDSONLY,
        PRIVATE
    }
}
