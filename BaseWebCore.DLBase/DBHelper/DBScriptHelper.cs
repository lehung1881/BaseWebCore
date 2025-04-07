using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseWebCore.DLBase
{
    public class DBScriptHelper
    {
        public DynamicParameters param { get; set; } = new DynamicParameters();

        public string script { get; set; } = string.Empty;

        public Dictionary<string, Type> types { get; set; }

        public void AppendParam(string param, object? value)
        {
            this.param.Add(param, value);
        }

        public void AppendScript(string script)
        {
            if (string.IsNullOrEmpty(this.script))
            {
                this.script = script;
            }
            else
            {
                this.script = $"{this.script} ; {script}";
            }
        }

        public void AppendType(string key, Type type)
        {
            if(this.types == null)
            {
                this.types = new Dictionary<string, Type>();
            }

            if(!this.types.ContainsKey(key))
            {
                this.types.Add(key, type);
            }
        }
    }
}
