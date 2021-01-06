namespace Tasker.Models.Configuration
{
    public interface ISwitchWithNightSelector
    {
        public bool OnlyWhenIsDark { get; set; }
        
        public bool OnlyWhenIsNight { get; set; }
    }
}