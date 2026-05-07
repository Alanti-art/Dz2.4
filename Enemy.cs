using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dz2._4
{
    internal class Enemy
    {
        private int curHp;
        private bool isDead;

        public int CurHp
        {
            get 
            { return curHp; }
            set 
            { curHp = value; }
        }

        public bool IsDead
        {
            get 
            { return isDead; }
            set 
            { isDead = value; }
        }

        public Enemy(int curHp, bool isDead)
        {
            this.curHp = curHp;
            this.isDead = isDead;
        }

        public void TakeDamage(int damage)
        {
            curHp -= damage;
            if (curHp <= 0)
            {
                curHp = 0;
                isDead = true;
            }
        }
    }
}