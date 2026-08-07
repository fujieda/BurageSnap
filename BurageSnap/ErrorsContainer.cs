// Copyright (C) 2016 Kazuhiro Fujieda <fujieda@users.osdn.me>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;

namespace BurageSnap;

/// <summary>
/// Prism.Mvvm.ErrorsContainer&lt;T&gt;互換の自作実装
/// </summary>
public class ErrorsContainer<T>
{
    private readonly Action<string> _raiseErrorsChanged;
    private readonly Dictionary<string, List<T>> _errors = new Dictionary<string, List<T>>();

    public ErrorsContainer(Action<string> raiseErrorsChanged)
    {
        _raiseErrorsChanged = raiseErrorsChanged;
    }

    public bool HasErrors
    {
        get
        {
            foreach (var errors in _errors.Values)
            {
                if (errors.Count > 0)
                    return true;
            }
            return false;
        }
    }

    public void SetErrors(string propertyName, IEnumerable<T> newErrors)
    {
        var list = new List<T>(newErrors);
        if (list.Count == 0)
        {
            ClearErrors(propertyName);
            return;
        }
        _errors[propertyName] = list;
        _raiseErrorsChanged(propertyName);
    }

    public void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            _raiseErrorsChanged(propertyName);
        }
    }

    public IEnumerable GetErrors(string propertyName)
    {
        return _errors.TryGetValue(propertyName ?? "", out var list) ? list : (IEnumerable)new List<T>();
    }
}
