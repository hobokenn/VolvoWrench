using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VolvoWrench.Demo_Stuff.GoldSource
{
    /// <summary>
    /// Stores strings with color information, batching same color strings
    /// Minimize color change operations when appending to textbox
    /// </summary>
    public class ColoredTextBuffer
    {
        private class TextColorPair
        {
            public string Text { get; set; }
            public Color Color { get; set; }

            public TextColorPair(string text, Color color)
            {
                Text = text;
                Color = color;
            }
        }

        private readonly List<TextColorPair> _entries = new List<TextColorPair>();
        private readonly Color _defaultColor;

        public ColoredTextBuffer(Color defaultColor)
        {
            _defaultColor = defaultColor;
        }

        /// <summary>
        /// Adds text with the specified color to the buffer.
        /// If the color matches the previous entry, text will be concatenated.
        /// </summary>
        public void Append(string text, Color? color = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Color actualColor = color ?? _defaultColor;

            if (_entries.Count > 0 && _entries[_entries.Count - 1].Color.Equals(actualColor))
            {
                // Same color as the last entry concatenates
                _entries[_entries.Count - 1].Text += text;
            }
            else
            {
                // Different color, create new entry
                _entries.Add(new TextColorPair(text, actualColor));
            }
        }

        /// <summary>
        /// Appends all the stored text to the specified RichTextBox
        /// with minimal color change operations
        /// </summary>
        public void AppendToRichTextBox(RichTextBox box)
        {
            if (box == null || _entries.Count == 0)
                return;

            box.SuspendLayout();

            // Store original selection
            int originalSelectionStart = box.SelectionStart;
            int originalSelectionLength = box.SelectionLength;

            // Move to the end
            box.SelectionStart = box.TextLength;

            foreach (var entry in _entries)
            {
                box.SelectionColor = entry.Color;
                box.AppendText(entry.Text);
            }

            // Restore selection color and optionally selection
            box.SelectionColor = box.ForeColor;

            // Optionally restore selection
            box.SelectionStart = originalSelectionStart;
            box.SelectionLength = originalSelectionLength;

            box.ResumeLayout();
        }

        /// <summary>
        /// Clears all stored text
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>
        /// Gets the total length of all text in the buffer
        /// </summary>
        public int Length
        {
            get
            {
                int totalLength = 0;
                foreach (var entry in _entries)
                {
                    totalLength += entry.Text.Length;
                }
                return totalLength;
            }
        }
    }
}