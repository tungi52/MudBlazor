﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions;
using MudBlazor.Utilities;

namespace MudBlazor
{
    public abstract partial class MudBaseDatePicker : MudBasePicker
    {
        [Inject] IJSRuntime JsRuntime { get; set; }
        /// <summary>
        /// Max selectable date.
        /// </summary>
        [Parameter] public DateTime? MaxDate { get; set; }

        /// <summary>
        /// Max selectable date.
        /// </summary>
        [Parameter] public DateTime? MinDate { get; set; }

        /// <summary>
        /// First view to show in the MudDatePicker.
        /// </summary>
        [Parameter] public OpenTo OpenTo { get; set; } = OpenTo.Date;

        /// <summary>
        /// Sets the Input Icon.
        /// </summary>
        [Parameter] public string InputIcon { get; set; } = Icons.Material.Event;

        /// <summary>
        /// String Format for selected date view
        /// </summary>
        [Parameter] public string DateFormat { get; set; }

        /// <summary>
        /// Defines on which day the week starts. Depends on the value of Culture. 
        /// </summary>
        [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; } = null;

        /// <summary>
        /// The current month of the date picker (two-way bindable). This changes when the user browses through the calender.
        /// The month is represented as a DateTime which is always the first day of that month. You can also set this to define which month is initially shown. If not set, the current month is shown.
        /// </summary>
        [Parameter]
        public DateTime? PickerMonth
        {
            get => _picker_month;
            set
            {
                if (value == _picker_month)
                    return;
                _picker_month = value;
                InvokeAsync(StateHasChanged);
                PickerMonthChanged.InvokeAsync(value);
            }
        }

        private DateTime? _picker_month;

        /// <summary>
        /// Fired when the date changes.
        /// </summary>
        [Parameter] public EventCallback<DateTime?> PickerMonthChanged { get; set; }

        /// <summary>
        /// The display culture
        /// </summary>
        [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

        /// <summary>
        /// Milliseconds to wait before closing the picker. This helps the user see that the date was selected before the popover disappears.
        /// </summary>
        [Parameter] public int ClosingDelay { get; set; } = 100;

        /// <summary>
        /// Number of months to display in the calendar
        /// </summary>
        [Parameter] public int DisplayMonths { get; set; } = 1;

        /// <summary>
        /// Maximum number of months in one row
        /// </summary>
        [Parameter] public int? MaxMonthColumns { get; set; }

        /// <summary>
        /// Start month when opening the picker. 
        /// </summary>
        [Parameter] public DateTime? StartMonth { get; set; }

        /// <summary>
        /// Display week numbers according to the <see cref="Culture" /> parameter. If no culture is defined, CultureInfo.CurrentCulture will be used.
        /// </summary>
        [Parameter] public bool ShowWeekNumbers { get; set; }

        /// <summary>
        /// Reference to the Picker, initialized via @ref
        /// </summary>
        protected MudPicker Picker;

        protected virtual bool IsRange { get; } = false;

        private OpenTo _currentView = OpenTo.Date;
        
        private void OnPickerOpened()
        {
            _currentView = OpenTo;
            if(_currentView == OpenTo.Year)
                _scrollToYearAfterRender=true;
        }
        
        /// <summary>
        /// Get the first of the month to display
        /// </summary>
        /// <returns></returns>
        protected DateTime GetMonthStart(int month) => _picker_month == null ? DateTime.Today.AddMonths(month).StartOfMonth() : _picker_month.Value.AddMonths(month);

        /// <summary>
        /// Get the last of the month to display
        /// </summary>
        /// <returns></returns>
        protected DateTime GetMonthEnd(int month) => _picker_month == null ? DateTime.Today.AddMonths(month).EndOfMonth() : _picker_month.Value.AddMonths(month).EndOfMonth();

        protected DayOfWeek GetFirstDayOfWeek()
        {
            if (FirstDayOfWeek.HasValue)
                return FirstDayOfWeek.Value;
            return Culture.DateTimeFormat.FirstDayOfWeek;
        }

        /// <summary>
        /// Gets the n-th week of the currently displayed month. 
        /// </summary>
        /// <param name="month">offset from _picker_month</param>
        /// <param name="index">between 0 and 4</param>
        /// <returns></returns>
        protected IEnumerable<DateTime> GetWeek(int month, int index)
        {
            if (index < 0 || index > 5)
                throw new ArgumentException("Index must be between 0 and 5");
            var month_first = GetMonthStart(month);
            var week_first = month_first.AddDays(index*7).StartOfWeek(GetFirstDayOfWeek( ));
            for (int i = 0; i < 7; i++)
                yield return week_first.AddDays(i);
        }

        private string GetWeekNumber(int month, int index)
        {
            if (index < 0 || index > 5)
                throw new ArgumentException("Index must be between 0 and 5");
            var month_first = GetMonthStart(month);
            var week_first = month_first.AddDays(index * 7).StartOfWeek(GetFirstDayOfWeek());
            for (int i = 0; i < 7; i++)
            {
                if (week_first.AddDays(i).Month == month_first.Month)
                    break;
                else if (i == 6)
                    return "";
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(week_first,
                Culture.DateTimeFormat.CalendarWeekRule, FirstDayOfWeek ?? Culture.DateTimeFormat.FirstDayOfWeek).ToString();
        }

        protected abstract string GetDayClasses(int month, DateTime day);

        /// <summary>
        /// User clicked on a day
        /// </summary>
        protected abstract void OnDayClicked(DateTime dateTime);

        protected virtual void OnMouseOver(DateTime time) { }

        /// <summary>
        /// return Mo, Tu, We, Th, Fr, Sa, Su in the right culture
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<string> GetAbbreviatedDayNames()
        {
            string[] dayNamesNormal = Culture.DateTimeFormat.AbbreviatedDayNames;
            string[] dayNamesShifted = Shift(dayNamesNormal, (int)GetFirstDayOfWeek());
            return dayNamesShifted;
        }

        /// <summary>
        /// Shift array and cycle around from the end
        /// </summary>
        private static T[] Shift<T>(T[] array, int positions)
        {
            T[] copy = new T[array.Length];
            Array.Copy(array, 0, copy, array.Length - positions, positions);
            Array.Copy(array, positions, copy, 0, array.Length - positions);
            return copy;
        }

        protected string GetMonthName(int month)
        {
            return GetMonthStart(month).ToString(Culture.DateTimeFormat.YearMonthPattern, Culture);
        }

        protected abstract string GetFormattedDateString();

        protected string GetFormattedYearString()
        {
            return GetMonthStart(0).ToString("yyyy", Culture);
        }

        private void OnPreviousMonthClick()
        {
            PickerMonth = GetMonthStart(0).AddDays(-1).StartOfMonth();
        }

        private void OnNextMonthClick()
        {
            PickerMonth = GetMonthEnd(0).AddDays(1);
        }

        private void OnPreviousYearClick()
        {
            PickerMonth = GetMonthStart(0).AddYears(-1);
        }

        private void OnNextYearClick()
        {
            PickerMonth = GetMonthStart(0).AddYears(1);
        }

        private void OnYearClick()
        {
            _currentView = OpenTo.Year;
            StateHasChanged();
            _scrollToYearAfterRender = true;
        }

        /// <summary>
        /// We need a random id for the year items in the year list so we can scroll to the item safely in every DatePicker.
        /// </summary>
        private string _componentId = Guid.NewGuid().ToString();

        /// <summary>
        /// Is set to true to scroll to the actual year after the next render
        /// </summary>
        private bool _scrollToYearAfterRender = false;

        public async void ScrollToYear()
        {
            _scrollToYearAfterRender = false;
            string id = $"{_componentId}{GetMonthStart(0).Year.ToString()}";
            await JsRuntime.InvokeVoidAsync("scrollHelpers.scrollToYear", id);
            StateHasChanged();
        }

        private int GetMinYear()
        {
            if (MinDate.HasValue)
                return MinDate.Value.Year;
            return DateTime.Today.Year - 100;
        }

        private int GetMaxYear()
        {
            if (MaxDate.HasValue)
                return MaxDate.Value.Year;
            return DateTime.Today.Year + 100;
        }

        private string GetYearClasses(int year)
        {
            if (year == GetMonthStart(0).Year)
                return $"mud-picker-year-selected mud-{Color.ToDescriptionString()}-text";
            return null;
        }

        private string GetCalendarHeaderClasses(int month)
        {
            return new CssBuilder("mud-picker-calendar-header")
                .AddClass($"mud-picker-calendar-header-{month + 1}")
                .AddClass($"mud-picker-calendar-header-last", month == DisplayMonths - 1)
                .Build();
        }

        private Typo GetYearTypo(int year)
        {
            if (year == GetMonthStart(0).Year)
                return Typo.h5;
            return Typo.subtitle1;
        }

        private void OnFormattedDateClick()
        {
            // todo: raise an event the user can handdle
        }

        private void OnYearClicked(int year)
        {
            _currentView = OpenTo.Month;
            var current = GetMonthStart(0);
            PickerMonth = new DateTime(year, current.Month,  1);
        }

        private IEnumerable<DateTime> GetAllMonths()
        {
            var current = GetMonthStart(0);
            for (int i = 1; i <= 12; i++)
                yield return new DateTime(current.Year, i, 1);
        }

        private string GetAbbreviatedMontName(in DateTime month)
        {
            return Culture.DateTimeFormat.AbbreviatedMonthNames[month.Month-1];
        }

        private string GetMonthClasses(DateTime month)
        {
            if (GetMonthStart(0) == month)
                return $"mud-picker-month-selected mud-color-text-{Color.ToDescriptionString()}";
            return null;
        }

        private Typo GetMonthTypo(in DateTime month)
        {
            if (GetMonthStart(0) == month)
                return Typo.h5;
            return Typo.subtitle1;
        }

        private void OnMonthClicked(int month)
        {
            _currentView = OpenTo.Month;
            _picker_month = _picker_month?.AddMonths(month);
            StateHasChanged();
        }

        private void OnMonthSelected(in DateTime month)
        {
            _currentView = OpenTo.Date;
            PickerMonth = month;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (_picker_month == null)
                    _picker_month = StartMonth?.StartOfMonth() ?? GetMonthStart(0);
            }

            if (firstRender && _currentView == OpenTo.Year)
            {
                ScrollToYear();
                return;
            }

            if (_scrollToYearAfterRender)
                ScrollToYear();
            await base.OnAfterRenderAsync(firstRender);
        }

    }
}
