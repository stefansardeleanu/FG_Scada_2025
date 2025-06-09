namespace FG_Scada_2025.Services
{
    public class NavigationService
    {
        public async Task NavigateToAsync(string route, Dictionary<string, object>? parameters = null)
        {
            try
            {
                if (parameters != null && parameters.Count > 0)
                {
                    await Shell.Current.GoToAsync(route, parameters);
                }
                else
                {
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        public async Task GoBackAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigation back error: {ex.Message}");
            }
        }
    }
}