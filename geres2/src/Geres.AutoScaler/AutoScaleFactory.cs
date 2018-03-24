//
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//           http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using Geres.AutoScaler.Interfaces;
using Geres.AutoScaler.Interfaces.AutoScalerPolicy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.AutoScaler
{
    public static class AutoScaleFactory
    {
        public static IAutoScalerHandler CreateAutoScaler(IDictionary<string, string> resourceDetails, IAutoScalerPolicy policy)
        {
            var autoScaler = new Handlers.AutoScalerHandler();
            autoScaler.Initialize(resourceDetails, policy);
            return autoScaler;
        }
    }
}
