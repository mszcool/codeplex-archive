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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Diagnostics
{
    public static class GeresAssertionHelper
    {
        public static void AssertNull(object o, string paramName) 
        {
            if (o == null)
                throw new ArgumentException(string.Format("Parameter cannot be null: {0}", paramName));
        }

        public static void AssertNullOrEmpty(string s, string paramName)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException(string.Format("Parameter cannot be null or empty: {0}", paramName));
        }

        public static void AssertNullOrEmptyOrWhitespace(string s, string paramName)
        {
            AssertNullOrEmpty(s, paramName);
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException(string.Format("Parameter cannot be just whitespace: {0}", paramName));
        }

        public static void AssertLength(string s, string paramName, int minLen, int maxLen)
        {
            var errorMessage = string.Format("Parameter-length must be greater than {0} and less than {1}: {2}", minLen, maxLen, paramName);
            if (string.IsNullOrEmpty(s) && minLen > 0)
                throw new ArgumentException(errorMessage);
            if ((s.Length > maxLen) || (s.Length < minLen))
                throw new ArgumentException(errorMessage);
        }
    }
}
