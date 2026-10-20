using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEMORA.Models;
using LEMORA.Services;

namespace LEMORA.ViewModels
{
    public class PlannerDay : ObservableObject
    {
        private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];

        private bool _isSelected;

        public DateTime Date { get; }
        public bool IsCurrentMonth { get; }
        public bool IsToday => Date == DateTime.Today;
        public OutfitPlan? Plan { get; }
        public string DayNumber => Date.Day.ToString(CultureInfo.InvariantCulture);
        public string ActivityLabel => Plan?.Activity ?? "";
        public string TodayLabel => IsToday ? "TODAY" : "";
        public string PlanMarker => Plan is null ? "" : "●";
        public Brush MarkerColor => ThemeBrush("AccentBrush");

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(CellBackground));
                    OnPropertyChanged(nameof(DayForeground));
                }
            }
        }

        public Brush CellBackground => IsSelected ? ThemeBrush("PrimaryBrush") :
            IsToday ? ThemeBrush("MelonBrush") : Plan is not null ? ThemeBrush("WarmPanelBrush") :
            IsCurrentMonth ? ThemeBrush("ElevatedBrush") : ThemeBrush("SurfaceBrush");

        public Brush DayForeground => IsCurrentMonth
            ? ThemeBrush("TextBrush") : ThemeBrush("MutedTextBrush");

        public PlannerDay(DateTime date, bool isCurrentMonth, OutfitPlan? plan)
        {
            Date = date.Date;
            IsCurrentMonth = isCurrentMonth;
            Plan = plan;
        }
    }

    public class OutfitPlannerViewModel : ObservableObject
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
        private readonly OutfitPlanService _planService = new();
        private readonly OutfitService _outfitService = new();
        private List<OutfitPlan> _allPlans = new();
        private DateTime _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _selectedDate = DateTime.Today;
        private Outfit? _selectedOutfit;
        private OutfitPlan? _selectedPlan;
        private string _activity = "";
        private string _notes = "";
        private string _feedback = "";
        private bool _isLoading;

        public ObservableCollection<PlannerDay> CalendarDays { get; } = new();
        public ObservableCollection<Outfit> SavedOutfits { get; } = new();
        public ObservableCollection<OutfitPlan> UpcomingPlans { get; } = new();
        public ObservableCollection<OutfitItem> SelectedPreviewItems { get; } = new();
        public string[] WeekDays { get; } = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

        public string VisibleMonthLabel => _visibleMonth.ToString("MMMM yyyy", English);
        public string SelectedDateLabel => SelectedDate.ToString("dddd, MMMM d, yyyy", English);
        public int UpcomingCount => UpcomingPlans.Count;
        public bool HasNoUpcoming => UpcomingCount == 0;
        public bool HasSavedOutfits => SavedOutfits.Count > 0;
        public bool HasNoSavedOutfits => !HasSavedOutfits;
        public bool HasSelectedPlan => SelectedPlan is not null;
        public bool HasNoSelectedPlan => !HasSelectedPlan;
        public string SaveButtonLabel => HasSelectedPlan ? "Update Schedule" : "Save to Calendar";
        public string ScheduledOutfitName => SelectedPlan?.Outfit.Name ?? "No outfit planned yet";
        public string ScheduledActivity => SelectedPlan?.Activity ?? "Choose a saved outfit to plan this day.";

        public DateTime SelectedDate
        {
            get => _selectedDate;
            private set
            {
                if (SetProperty(ref _selectedDate, value.Date))
                    OnPropertyChanged(nameof(SelectedDateLabel));
            }
        }

        public Outfit? SelectedOutfit
        {
            get => _selectedOutfit;
            set => SetProperty(ref _selectedOutfit, value);
        }

        public OutfitPlan? SelectedPlan
        {
            get => _selectedPlan;
            private set
            {
                if (SetProperty(ref _selectedPlan, value))
                {
                    OnPropertyChanged(nameof(HasSelectedPlan));
                    OnPropertyChanged(nameof(HasNoSelectedPlan));
                    OnPropertyChanged(nameof(SaveButtonLabel));
                    OnPropertyChanged(nameof(ScheduledOutfitName));
                    OnPropertyChanged(nameof(ScheduledActivity));
                }
            }
        }

        public string Activity
        {
            get => _activity;
            set => SetProperty(ref _activity, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string Feedback
        {
            get => _feedback;
            set => SetProperty(ref _feedback, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public IRelayCommand PreviousMonthCommand { get; }
        public IRelayCommand NextMonthCommand { get; }
        public IRelayCommand TodayCommand { get; }
        public IRelayCommand<PlannerDay> SelectDayCommand { get; }
        public IRelayCommand<OutfitPlan> SelectUpcomingCommand { get; }
        public IAsyncRelayCommand SavePlanCommand { get; }

        public OutfitPlannerViewModel()
        {
            PreviousMonthCommand = new RelayCommand(() => MoveMonth(-1));
            NextMonthCommand = new RelayCommand(() => MoveMonth(1));
            TodayCommand = new RelayCommand(() => SelectDate(DateTime.Today));
            SelectDayCommand = new RelayCommand<PlannerDay>(day =>
            {
                if (day is not null) SelectDate(day.Date);
            });
            SelectUpcomingCommand = new RelayCommand<OutfitPlan>(plan =>
            {
                if (plan is not null) SelectDate(plan.Date);
            });
            SavePlanCommand = new AsyncRelayCommand(SaveAsync);
            BuildCalendar();
            SelectDate(DateTime.Today);
        }

        private void MoveMonth(int step)
        {
            _visibleMonth = _visibleMonth.AddMonths(step);
            OnPropertyChanged(nameof(VisibleMonthLabel));
            BuildCalendar();
            SelectDate(_visibleMonth);
        }

        private void SelectDate(DateTime day)
        {
            DateTime date = day.Date;
            if (date.Year != _visibleMonth.Year || date.Month != _visibleMonth.Month)
            {
                _visibleMonth = new DateTime(date.Year, date.Month, 1);
                OnPropertyChanged(nameof(VisibleMonthLabel));
                BuildCalendar();
            }

            SelectedDate = date;
            foreach (var calendarDay in CalendarDays)
                calendarDay.IsSelected = calendarDay.Date == date;

            SelectedPlan = _allPlans.FirstOrDefault(plan => plan.Date.Date == date);
            SelectedOutfit = SelectedPlan is null
                ? null : SavedOutfits.FirstOrDefault(outfit => outfit.Id == SelectedPlan.OutfitId);
            Activity = SelectedPlan?.Activity ?? "";
            Notes = SelectedPlan?.Notes ?? "";

            SelectedPreviewItems.Clear();
            if (SelectedPlan is not null)
            {
                foreach (var item in SelectedPlan.Outfit.Items.OrderBy(x => x.Slot))
                    SelectedPreviewItems.Add(item);
            }

            Feedback = "";
        }

        private void BuildCalendar()
        {
            CalendarDays.Clear();
            int offset = ((int)_visibleMonth.DayOfWeek + 6) % 7;
            DateTime firstShown = _visibleMonth.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                DateTime date = firstShown.AddDays(i);
                bool inMonth = date.Month == _visibleMonth.Month && date.Year == _visibleMonth.Year;
                var plan = _allPlans.FirstOrDefault(x => x.Date.Date == date.Date);
                CalendarDays.Add(new PlannerDay(date, inMonth, plan)
                {
                    IsSelected = date.Date == SelectedDate.Date
                });
            }
        }

        public async Task RefreshAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var outfits = await _outfitService.GetAllAsync();
                SavedOutfits.Clear();
                foreach (var outfit in outfits) SavedOutfits.Add(outfit);
                OnPropertyChanged(nameof(HasSavedOutfits));
                OnPropertyChanged(nameof(HasNoSavedOutfits));

                await ReloadPlansAsync();
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to load Outfit Planner: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReloadPlansAsync()
        {
            _allPlans = await _planService.GetAllAsync();
            UpcomingPlans.Clear();
            foreach (var plan in _allPlans.Where(x => x.Date.Date >= DateTime.Today).Take(6))
                UpcomingPlans.Add(plan);

            OnPropertyChanged(nameof(UpcomingCount));
            OnPropertyChanged(nameof(HasNoUpcoming));
            BuildCalendar();
            SelectDate(SelectedDate);
        }

        private async Task SaveAsync()
        {
            if (IsLoading) return;
            if (SelectedOutfit is null)
            {
                Feedback = "Select a saved outfit first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Activity))
            {
                Feedback = "Enter the activity for this day.";
                return;
            }

            IsLoading = true;
            try
            {
                bool replacing = HasSelectedPlan;
                await _planService.SaveAsync(SelectedDate, SelectedOutfit.Id, Activity, Notes);
                await ReloadPlansAsync();
                Feedback = replacing ? "Schedule updated successfully!" : "Outfit scheduled successfully!";
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to save schedule: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedPlan is null || IsLoading) return;
            IsLoading = true;
            try
            {
                await _planService.DeleteAsync(SelectedDate);
                await ReloadPlansAsync();
                Feedback = "Schedule removed successfully!";
            }
            catch (Exception ex)
            {
                Feedback = $"Unable to remove schedule: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
