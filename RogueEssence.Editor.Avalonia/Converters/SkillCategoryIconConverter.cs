using System;
using System.Globalization;
using Avalonia.Data.Converters;
using RogueEssence.Data;

namespace RogueEssence.Dev.Converters
{
    public class SkillCategoryIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BattleData.SkillCategory category = (BattleData.SkillCategory)value;
            return DevDataManager.GetSkillCategoryIcon(category);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}