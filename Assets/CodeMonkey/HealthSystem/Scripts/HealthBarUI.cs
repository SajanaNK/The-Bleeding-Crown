using UnityEngine;
using UnityEngine.UI;

namespace CodeMonkey.HealthSystemCM {

    /// <summary>
    /// Simple UI Health Bar, sets the Image fillAmount based on the linked HealthSystem
    /// Check the Demo scene for a usage example
    /// </summary>
    public class HealthBarUI : MonoBehaviour {

        [Tooltip("Optional; Either assign a reference in the Editor (that implements IGetHealthSystem) or manually call SetHealthSystem()")]
        [SerializeField] private GameObject getHealthSystemGameObject;

        [Tooltip("Image to show the Health Bar, should be set as Fill, the script modifies fillAmount")]
        [SerializeField] private Image image;


        [Tooltip("Fill units per second the bar animates towards its target, instead of snapping instantly")]
        [SerializeField] private float fillSpeed = 1.5f;

        private HealthSystem healthSystem;
        private float targetFillAmount;


        private void Start() {
            if (HealthSystem.TryGetHealthSystem(getHealthSystemGameObject, out HealthSystem healthSystem)) {
                SetHealthSystem(healthSystem);
            }
        }

        private void Update() {
            if (image.fillAmount != targetFillAmount) {
                image.fillAmount = Mathf.MoveTowards(image.fillAmount, targetFillAmount, fillSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Set the Health System for this Health Bar
        /// </summary>
        public void SetHealthSystem(HealthSystem healthSystem) {
            if (this.healthSystem != null) {
                this.healthSystem.OnHealthChanged -= HealthSystem_OnHealthChanged;
            }
            this.healthSystem = healthSystem;

            UpdateHealthBar();

            healthSystem.OnHealthChanged += HealthSystem_OnHealthChanged;
        }

        /// <summary>
        /// Event fired from the Health System when Health Amount changes, update Health Bar
        /// </summary>
        private void HealthSystem_OnHealthChanged(object sender, System.EventArgs e) {
            UpdateHealthBar();
        }

        /// <summary>
        /// Update Health Bar using the Image fillAmount based on the current Health Amount
        /// </summary>
        private void UpdateHealthBar() {
            targetFillAmount = healthSystem.GetHealthNormalized();
        }

        /// <summary>
        /// Clean up events when this Game Object is destroyed
        /// </summary>
        private void OnDestroy() {
            healthSystem.OnHealthChanged -= HealthSystem_OnHealthChanged;
        }

    }

}