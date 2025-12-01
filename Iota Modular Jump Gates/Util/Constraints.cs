using ProtoBuf;
using System;

namespace IOTA.ModularJumpGates.Util
{


	/// <summary>
	/// Interface representing a value constrant
	/// </summary>
	/// <typeparam name="T">The typename; must implement IComparable</typeparam>
	public interface ConstraintValue<T> where T : IComparable<T>
	{
		#region Public Methods
		/// <summary>
		/// Checks if the specified value matches this constrant
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <returns>true if value matches constraint</returns>
		bool Match(T value);
		#endregion
	}

	/// <summary>
	/// Class representing a numerical range
	/// </summary>
	/// <typeparam name="T">The typename; must implement IComparable</typeparam>
	[ProtoContract]
	public struct NumberRange<T> : ConstraintValue<T> where T : IComparable<T>
	{
		#region Public Variables
		/// <summary>
		/// The lower value of this range
		/// </summary>
		[ProtoMember(1)]
		public T LowerBound;

		/// <summary>
		/// The upper value of this range
		/// </summary>
		[ProtoMember(2)]
		public T UpperBound;

		/// <summary>
		/// Whether the lower value is inclusive
		/// </summary>
		[ProtoMember(3)]
		public bool LowerInclusive;

		/// <summary>
		/// Whether the upper value is inclusive
		/// </summary>
		[ProtoMember(4)]
		public bool UpperInclusive;
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Creates a single value
		/// </summary>
		/// <param name="value">The single value</param>
		/// <returns>The range [value, value]</returns>
		public static NumberRange<T> Single(T value)
		{
			return new NumberRange<T>(value, value, true, true);
		}

		/// <summary>
		/// Creates an inclusive-inclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <returns>The range [min, max]</returns>
		public static NumberRange<T> RangeII(T inclusive_min, T inclusive_max)
		{
			return new NumberRange<T>(inclusive_min, inclusive_max, true, true);
		}

		/// <summary>
		/// Creates an exclusive-inclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <returns>The range (min, max]</returns>
		public static NumberRange<T> RangeEI(T exclusive_min, T inclusive_max)
		{
			return new NumberRange<T>(exclusive_min, inclusive_max, false, true);
		}

		/// <summary>
		/// Creates an inclusive-exclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <returns>The range [min, max)</returns>
		public static NumberRange<T> RangeIE(T inclusive_min, T exclusive_max)
		{
			return new NumberRange<T>(inclusive_min, exclusive_max, true, false);
		}

		/// <summary>
		/// Creates an exclusive-exclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <returns>The range (min, max)</returns>
		public static NumberRange<T> RangeEE(T exclusive_min, T exclusive_max)
		{
			return new NumberRange<T>(exclusive_min, exclusive_max, false, false);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new number ange
		/// </summary>
		/// <param name="min">The minimum value</param>
		/// <param name="max">The maximum value</param>
		/// <param name="lower_inclusive">Whether the minimum is inclusive</param>
		/// <param name="upper_inclusive">Whether the maximum is inclusive</param>
		private NumberRange(T min, T max, bool lower_inclusive, bool upper_inclusive)
		{
			this.LowerBound = min;
			this.UpperBound = max;
			this.LowerInclusive = lower_inclusive;
			this.UpperInclusive = upper_inclusive;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks if the specified value is within this range
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <returns>true if value in range</returns>
		public bool Match(T value)
		{
			int lmatch = value.CompareTo(this.LowerBound);
			int umatch = value.CompareTo(this.UpperBound);
			return (this.LowerInclusive && lmatch >= 0 || lmatch > 0) && (this.UpperInclusive && umatch <= 0 || umatch < 0);
		}
		#endregion
	}

	/// <summary>
	/// Class representing a date-time range
	/// </summary>
	/// <typeparam name="T">The typename; must implement IComparable</typeparam>
	[ProtoContract]
	public struct DateTimeRange : ConstraintValue<DateTime>
	{
		public enum ComponentCompareType : byte
		{
			MILLISECOND = 1,
			SECOND = 2,
			MINUTE = 4,
			HOUR = 8,
			DAY = 16,
			MONTH = 32,
			YEAR = 64,
			ALL = 0xFF,
		}

		#region Public Variables
		/// <summary>
		/// The lower value of this range
		/// </summary>
		[ProtoMember(1)]
		public DateTime LowerBound;

		/// <summary>
		/// The upper value of this range
		/// </summary>
		[ProtoMember(2)]
		public DateTime UpperBound;

		/// <summary>
		/// Whether the lower value is inclusive
		/// </summary>
		[ProtoMember(3)]
		public bool LowerInclusive;

		/// <summary>
		/// Whether the upper value is inclusive
		/// </summary>
		[ProtoMember(4)]
		public bool UpperInclusive;

		/// <summary>
		/// The date-time compontents to compare
		/// </summary>
		[ProtoMember(5)]
		public ComponentCompareType ComponentCompare;
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Creates a single value
		/// </summary>
		/// <param name="value">The single value</param>
		/// <param name="component_compare">The date-time components to compare</param>
		/// <returns>The range [value, value]</returns>
		public static DateTimeRange Single(DateTime value, ComponentCompareType component_compare = ComponentCompareType.ALL)
		{
			return new DateTimeRange(value, value, true, true, component_compare);
		}

		/// <summary>
		/// Creates an inclusive-inclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <param name="component_compare">The date-time components to compare</param>
		/// <returns>The range [min, max]</returns>
		public static DateTimeRange RangeII(DateTime inclusive_min, DateTime inclusive_max, ComponentCompareType component_compare = ComponentCompareType.ALL)
		{
			return new DateTimeRange(inclusive_min, inclusive_max, true, true, component_compare);
		}

		/// <summary>
		/// Creates an exclusive-inclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <param name="component_compare">The date-time components to compare</param>
		/// <returns>The range (min, max]</returns>
		public static DateTimeRange RangeEI(DateTime exclusive_min, DateTime inclusive_max, ComponentCompareType component_compare = ComponentCompareType.ALL)
		{
			return new DateTimeRange(exclusive_min, inclusive_max, false, true, component_compare);
		}

		/// <summary>
		/// Creates an inclusive-exclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <param name="component_compare">The date-time components to compare</param>
		/// <returns>The range [min, max)</returns>
		public static DateTimeRange RangeIE(DateTime inclusive_min, DateTime exclusive_max, ComponentCompareType component_compare = ComponentCompareType.ALL)
		{
			return new DateTimeRange(inclusive_min, exclusive_max, true, false, component_compare);
		}

		/// <summary>
		/// Creates an exclusive-exclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <param name="component_compare">The date-time components to compare</param>
		/// <returns>The range (min, max)</returns>
		public static DateTimeRange RangeEE(DateTime exclusive_min, DateTime exclusive_max, ComponentCompareType component_compare = ComponentCompareType.ALL)
		{
			return new DateTimeRange(exclusive_min, exclusive_max, false, false, component_compare);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new date-time range
		/// </summary>
		/// <param name="min">The minimum value</param>
		/// <param name="max">The maximum value</param>
		/// <param name="lower_inclusive">Whether the minimum is inclusive</param>
		/// <param name="upper_inclusive">Whether the maximum is inclusive</param>
		/// <param name="component_compare">The date-time components to compare</param>
		private DateTimeRange(DateTime min, DateTime max, bool lower_inclusive, bool upper_inclusive, ComponentCompareType component_compare)
		{
			this.LowerBound = min;
			this.UpperBound = max;
			this.LowerInclusive = lower_inclusive;
			this.UpperInclusive = upper_inclusive;
			this.ComponentCompare = component_compare;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks if the specified value is within this range
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <returns>true if value in range</returns>
		public bool Match(DateTime value)
		{
			bool result = true;

			if ((this.ComponentCompare & ComponentCompareType.MILLISECOND) != 0)
				result = result && (this.LowerInclusive && value.Millisecond >= this.LowerBound.Millisecond || value.Millisecond > this.LowerBound.Millisecond) && (this.UpperInclusive && value.Millisecond <= this.UpperBound.Millisecond || value.Millisecond < this.UpperBound.Millisecond);
			if ((this.ComponentCompare & ComponentCompareType.SECOND) != 0)
				result = result && (this.LowerInclusive && value.Second >= this.LowerBound.Second || value.Second > this.LowerBound.Second) && (this.UpperInclusive && value.Second <= this.UpperBound.Second || value.Second < this.UpperBound.Second);
			if ((this.ComponentCompare & ComponentCompareType.MINUTE) != 0)
				result = result && (this.LowerInclusive && value.Minute >= this.LowerBound.Minute || value.Minute > this.LowerBound.Minute) && (this.UpperInclusive && value.Minute <= this.UpperBound.Minute || value.Minute < this.UpperBound.Minute);
			if ((this.ComponentCompare & ComponentCompareType.HOUR) != 0)
				result = result && (this.LowerInclusive && value.Hour >= this.LowerBound.Hour || value.Hour > this.LowerBound.Hour) && (this.UpperInclusive && value.Hour <= this.UpperBound.Hour || value.Hour < this.UpperBound.Hour);
			if ((this.ComponentCompare & ComponentCompareType.DAY) != 0)
				result = result && (this.LowerInclusive && value.Day >= this.LowerBound.Day || value.Day > this.LowerBound.Day) && (this.UpperInclusive && value.Day <= this.UpperBound.Day || value.Day < this.UpperBound.Day);
			if ((this.ComponentCompare & ComponentCompareType.MONTH) != 0)
				result = result && (this.LowerInclusive && value.Month >= this.LowerBound.Month || value.Month > this.LowerBound.Month) && (this.UpperInclusive && value.Month <= this.UpperBound.Month || value.Month < this.UpperBound.Month);
			if ((this.ComponentCompare & ComponentCompareType.YEAR) != 0)
				result = result && (this.LowerInclusive && value.Year >= this.LowerBound.Year || value.Year > this.LowerBound.Year) && (this.UpperInclusive && value.Year <= this.UpperBound.Year || value.Year < this.UpperBound.Year);
			
			return result;
		}
		#endregion
	}
}
