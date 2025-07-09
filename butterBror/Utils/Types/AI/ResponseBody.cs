using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.AI
{
    /// <summary>
    /// Represents the complete response body from the AI API.
    /// </summary>
    internal class ResponseBody
    {
        /// <summary>
        /// Gets or sets the list of response choices from the AI model.
        /// </summary>
        public List<Choice> choices { get; set; }
    }
}
