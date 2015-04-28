using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace OneDrive
{
    public class drive
    {
        public string id;

        public string name;
        public string description;
        public string parent_id;

        /*{            
         "id":  "folder.69815dd3faad4564.69815DD3FAAD4564!105",                         
         "name":  "Документы",             
         "parent_id":  "folder.69815dd3faad4564",
        },*/
    }
    public class driveList
    {
        public List<drive> data { get; set; }
    }







}
