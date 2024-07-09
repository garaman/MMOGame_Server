using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Object
{
    public class Arrow : Projectile
    {
        public GameObject Owner {  get; set; }

        public void Update()
        {

        }
    }
}
