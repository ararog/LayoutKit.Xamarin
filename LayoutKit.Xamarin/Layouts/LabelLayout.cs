using CoreGraphics;
using Foundation;
using System;
using UIKit;

namespace LayoutKit.Xamarin
{
	public class LabelLayout : Layout
	{
        public delegate void ConfigDelegate(UIView view);

        public enum TextType
        {
            Unattributed,
            Attributed
        }

        public TextType textType;
        public int numberOfLines;
        public UIFont font;
        public Alignment alignment;
        public Flexibility flexibility;
        public ConfigDelegate config;

        private const int defaultNumberOfLines = 0;

        public LabelLayout(TextType textType,
                    int numberOfLines = defaultNumberOfLines,
                    UIFont font = null,
                    Alignment? alignment = null,
                    Flexibility? flexibility = null,
                    ConfigDelegate config = null) {

            if (alignment == null)
                this.alignment = Alignment.topLeading;
            else
                this.alignment = alignment.Value;

            if(flexibility == null)
                this.flexibility = Flexibility.flexible;
            else
                this.flexibility = flexibility.Value;

            if (font == null)
                this.font = UIFont.SystemFontOfSize(UIFont.LabelFontSize);
            else
                this.font = font;

            this.textType = textType;
            this.numberOfLines = numberOfLines;
            this.config = config;
        }

        // MARK: - Convenience initializers

        public LabelLayout(string text,
                            int numberOfLines = defaultNumberOfLines,
                            UIFont font = null,
                            Alignment? alignment = null,
                            Flexibility? flexibility = null,
                            ConfigDelegate config = null) :

            this(TextType.Unattributed,
                      numberOfLines,
                      font, alignment,
                      flexibility,
                      config) { 
        }

        public LabelLayout(NSAttributedString attributedText,
                        int numberOfLines = defaultNumberOfLines,
                        UIFont font = null,
                        Alignment? alignment = null,
                        Flexibility? flexibility = null,
                        ConfigDelegate config = null) :

            this(TextType.Attributed,
                      numberOfLines,
                      font, alignment,
                      flexibility,
                      config) {
        }

        // MARK: - Layout protocol

        public LayoutMeasurement Measurement(LayoutMeasurement maxSize) {
            var fittedSize = TextSize(maxSize);
            return new LayoutMeasurement(this, fittedSize.SizeDecreasedToSize(maxSize), maxSize, new LayoutMeasurement[] { });
        }

        private CGSize TextSize(CGSize maxSize) {
            NSStringDrawingOptions[] options = new NSStringDrawingOptions[] {
                NSStringDrawingOptions.UsesLineFragmentOrigin,
                NSStringDrawingOptions.UsesFontLeading
            };

            CGSize size;
            switch (textType) {
            case TextType.Attributed:
                if (attributedText == "") {
                    return CGSize.Empty;
                }

                // UILabel uses a default font if one is not specified in the attributed string.
                // boundingRectWithSize does not appear to have the same logic,
                // so we need to ensure that our attributed string has a default font.
                // We do this by creating a new attributed string with the default font and then
                // applying all of the attributes from the provided attributed string.
                var fontAttribute = NSFontAttributeName(font);
                var attributedTextWithFont = new NSMutableAttributedString(attributedText.string, fontAttribute);
                var fullRange = new NSRange(0, (attributedText.string as NSString).length);
                attributedTextWithFont.BeginEditing();
                attributedText.EnumerateAttributesInRange(fullRange, .LongestEffectiveRangeNotRequired, { (attributes, range, _) in
                    attributedTextWithFont.AddAttributes(attributes, range);
                });
                attributedTextWithFont.EndEditing();

                size = attributedTextWithFont.BoundingRectWithSize(maxSize, options, null).Size;
                break;
            case TextType.Unattributed:
                if (text == "") {
                    return CGSize.Empty;
                }
                size = text.BoundingRectWithSize(maxSize, options, [NSFontAttributeName: font], null).Size;
                break;
            }
            // boundingRectWithSize returns size to a precision of hundredths of a point,
            // but UILabel only returns sizes with a point precision of 1/screenDensity.
            size.Height = RoundUpToFractionalPoint(size.Height);
            size.Width = RoundUpToFractionalPoint(size.Width);
            if(numberOfLines > 0){
                var maxHeight = RoundUpToFractionalPoint(numberOfLines * font.LineHeight);
                if(size.Height > maxHeight){
                    size = new CGSize(maxSize.Width, maxHeight);
                }
            }
            return size;
        }

        private nfloat RoundUpToFractionalPoint(nfloat point) {
            if (point <= 0) {
                return 0;
            }
            nfloat scale = UIScreen.MainScreen.Scale;
            // The smallest precision in points (aka the number of points per hardware pixel).
            var pointPrecision = 1.0f / scale;
            if (point <= pointPrecision) {
                return pointPrecision;
            }
            return Math.Ceiling(point * scale) / scale;
        }

        public LayoutArrangement Arrangement(CGRect rect, LayoutMeasurement measurement) {
            var frame = alignment.Position(measurement.Size, rect);
            return new LayoutArrangement(this, frame, new LayoutArrangement[] { });
        }

        public UIView MakeView()  {
            var label = new UILabel();
            config(label);
            label.Lines = numberOfLines;
            label.Font = font;
            switch(textType) {
            case TextType.Unattributed:
                label.Text = text;
                break;
            case TextType.Attributed:
                label.AttributedText = attributedText;
                break;
            }
            return label;
        }
	}
}

