r'''
Copyright 2014 Google Inc. All rights reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
'''
import subprocess
import re


def get_interface_addresses():
    """Get all ip addresses assigned to interfaces.

    Returns a tuple of (v4 addresses, v6 addresses)
    """
    try:
        output = subprocess.check_output("ifconfig")
    except subprocess.CalledProcessError:
        # Couldn't call ifconfig. Best guess it.
        return (["127.0.0.1"], [])
    # Parse out the results.
    v4 = re.findall("inet (addr:)?([^ ]*)", output)
    v6 = re.findall("inet6 (addr: )?([^ ]*)", output)
    v4 = [e[1] for e in v4]
    v6 = [e[1] for e in v6]
    return v4, v6
