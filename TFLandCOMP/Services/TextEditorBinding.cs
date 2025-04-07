using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using AvaloniaEdit;

namespace TFLandCOMP.Services
{
    public static class TextEditorBinding
    {
        // Регистрируем прикреплённое свойство BindableText для TextEditor
        public static readonly AttachedProperty<string> BindableTextProperty =
            AvaloniaProperty.RegisterAttached<TextEditor, string>(
                "BindableText", typeof(TextEditorBinding), string.Empty);

        // Подписываемся на изменения значения BindableText
        static TextEditorBinding()
        {
            BindableTextProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is TextEditor editor)
                {
                    string newValue = args.NewValue.GetValueOrDefault<string>();
                    if (editor.Text != newValue)
                    {
                        // Чтобы избежать бесконечной рекурсии, временно отписываем обработчик
                        editor.TextChanged -= Editor_TextChanged;
                        editor.Text = newValue;
                        editor.TextChanged += Editor_TextChanged;
                    }
                }
            });
        }

        // Обработчик события изменения текста в редакторе
        private static void Editor_TextChanged(object sender, EventArgs e)
        {
            if (sender is TextEditor editor)
            {
                SetBindableText(editor, editor.Text);
            }
        }

        public static string GetBindableText(TextEditor editor)
        {
            return editor.GetValue(BindableTextProperty);
        }

        public static void SetBindableText(TextEditor editor, string value)
        {
            editor.SetValue(BindableTextProperty, value);
        }
    }
}
