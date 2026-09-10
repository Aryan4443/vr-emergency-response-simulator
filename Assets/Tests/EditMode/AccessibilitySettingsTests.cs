using NUnit.Framework;
using VRSim.Accessibility;

namespace VRSim.Tests
{
    public class AccessibilitySettingsTests
    {
        [Test]
        public void DefaultsAreComfortableAndConservative()
        {
            var settings = new AccessibilitySettings();

            Assert.IsFalse(settings.SeatedMode);
            Assert.IsTrue(settings.SubtitlesEnabled, "critical audio must be subtitled by default");
            Assert.IsTrue(settings.SnapTurnEnabled, "snap turning is the comfortable default");
            Assert.AreEqual(45f, settings.SnapTurnDegrees);
            Assert.AreEqual(1f, settings.TextScale);
            Assert.IsFalse(settings.HighContrast);
        }

        [Test]
        public void TextScaleIsClampedToAReadableRange()
        {
            var settings = new AccessibilitySettings();

            settings.TextScale = 0.1f;
            Assert.AreEqual(AccessibilitySettings.MinTextScale, settings.TextScale);

            settings.TextScale = 99f;
            Assert.AreEqual(AccessibilitySettings.MaxTextScale, settings.TextScale);
        }

        [Test]
        public void MovementSpeedIsClampedToAComfortableRange()
        {
            var settings = new AccessibilitySettings();

            settings.MovementSpeed = -5f;
            Assert.AreEqual(AccessibilitySettings.MinMovementSpeed, settings.MovementSpeed);

            settings.MovementSpeed = 100f;
            Assert.AreEqual(AccessibilitySettings.MaxMovementSpeed, settings.MovementSpeed);
        }

        [Test]
        public void SnapTurnDegreesIsClampedToASensibleRange()
        {
            var settings = new AccessibilitySettings();

            settings.SnapTurnDegrees = 1f;
            Assert.AreEqual(AccessibilitySettings.MinSnapTurnDegrees, settings.SnapTurnDegrees);

            settings.SnapTurnDegrees = 500f;
            Assert.AreEqual(AccessibilitySettings.MaxSnapTurnDegrees, settings.SnapTurnDegrees);
        }

        [Test]
        public void SeatedModeLowersTheRigInsteadOfMovingTheCamera()
        {
            var settings = new AccessibilitySettings { SeatedMode = true };

            Assert.IsTrue(settings.SeatedMode);
            Assert.AreEqual(AccessibilitySettings.SeatedEyeHeight, settings.EyeHeight);

            settings.SeatedMode = false;
            Assert.AreEqual(AccessibilitySettings.StandingEyeHeight, settings.EyeHeight);
        }

        [Test]
        public void RaisesChangedWheneverASettingIsUpdated()
        {
            var settings = new AccessibilitySettings();
            var changes = 0;
            settings.Changed += () => changes++;

            settings.TextScale = 1.5f;
            settings.HighContrast = true;
            settings.SeatedMode = true;

            Assert.AreEqual(3, changes);
        }

        [Test]
        public void DoesNotRaiseChangedWhenTheValueIsUnchanged()
        {
            var settings = new AccessibilitySettings();
            var changes = 0;
            settings.Changed += () => changes++;

            settings.TextScale = settings.TextScale;
            settings.HighContrast = settings.HighContrast;

            Assert.AreEqual(0, changes);
        }

        [Test]
        public void SurvivesAJsonRoundTrip()
        {
            var settings = new AccessibilitySettings
            {
                SeatedMode = true,
                SubtitlesEnabled = false,
                SnapTurnEnabled = false,
                SnapTurnDegrees = 30f,
                TextScale = 1.8f,
                HighContrast = true,
                MovementSpeed = 2.5f,
            };

            var restored = AccessibilitySettings.FromJson(settings.ToJson());

            Assert.IsTrue(restored.SeatedMode);
            Assert.IsFalse(restored.SubtitlesEnabled);
            Assert.IsFalse(restored.SnapTurnEnabled);
            Assert.AreEqual(30f, restored.SnapTurnDegrees);
            Assert.AreEqual(1.8f, restored.TextScale, 0.0001f);
            Assert.IsTrue(restored.HighContrast);
            Assert.AreEqual(2.5f, restored.MovementSpeed, 0.0001f);
        }

        [Test]
        public void RestoringFromRubbishJsonFallsBackToTheDefaults()
        {
            var restored = AccessibilitySettings.FromJson("not json at all");

            Assert.AreEqual(1f, restored.TextScale);
            Assert.IsTrue(restored.SubtitlesEnabled);
        }
    }
}
