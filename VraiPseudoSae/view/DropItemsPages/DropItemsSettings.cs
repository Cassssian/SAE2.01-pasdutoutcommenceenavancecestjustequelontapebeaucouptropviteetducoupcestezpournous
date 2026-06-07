using System;
using System.Collections.Generic;
using System.Text;

namespace VraiPseudoSae.view
{
    public class DropItemsSettings
    {
        /// <summary>
        /// Le décor, il est défini dans les Options, et on le garde ici.
        /// </summary>
        private DropItemsMapsEnum decor;

        /// <summary>
        /// Propriété qui permet de voir le décor sélectionné et de le modifier.
        /// </summary>
        public DropItemsMapsEnum Decor
        {
            get { return decor; }
            set { decor = value; }
        }


        public DropItemsSettings(DropItemsMapsEnum decor)
        {
            this.decor = decor;
        }
    }
}
