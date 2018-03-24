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
using Geres.AutoScaler.Interfaces.AutoScalerPolicy;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Common
{
    public class CompositionAutoScalerPolicyFactory : IAutoScalerPolicyFactory
    {
        private IEnumerable<Lazy<IAutoScalerPolicy>> _policies;

        public CompositionAutoScalerPolicyFactory(string folder)
        {
            var catalog = new DirectoryCatalog(folder, "*.dll");

            var container = new CompositionContainer(catalog);
            _policies = container.GetExports<IAutoScalerPolicy>();
        }

        public IAutoScalerPolicy Lookup(string policyType)
        {
            var lazy = _policies.SingleOrDefault(p => p.Value.PolicyType.Equals(policyType, StringComparison.CurrentCultureIgnoreCase));
            
            if (lazy != null)
            {
                return lazy.Value;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}
