using System;

namespace PX.Objects.AP
{
	public class Triplet<T1, T2, T3> : IComparable<Triplet<T1, T2, T3>>, IEquatable<Triplet<T1, T2, T3>>
        where T1 : IComparable<T1>
        where T2 : IComparable<T2>
        where T3 : IComparable<T3>
    {
        public Triplet(T1 aArg1, T2 aArg2, T3 aArg3)
        {
            this.first = aArg1;
            this.second = aArg2;
            this.third = aArg3;
        }
        public T1 first;
        public T2 second;
        public T3 third;

        #region IComparable<Triplet<T1,T2,T3>> Members
        public virtual int CompareTo(Triplet<T1, T2, T3> other)
        {
            int res = this.first.CompareTo(other.first);
            if (res == 0)
                res = this.second.CompareTo(other.second);
            if (res == 0)
                return this.third.CompareTo(other.third);
            return res;
        }

        #endregion

        #region IEquatable<Triplet<T1,T2,T3>> Members
        public override int GetHashCode()
        {
            return 0;
        }

        public virtual bool Equals(Triplet<T1, T2, T3> other)
        {
            return (this.CompareTo(other) == 0);
        }

        public override bool Equals(Object other)
        {
            Triplet<T1, T2, T3> tmp = other as Triplet<T1, T2, T3>;
            if (tmp != null)
                return (this.CompareTo(tmp) == 0);
            return ((Object)this).Equals(other);
        }

        #endregion
    }

}
