using UnityEngine;

namespace Platformer.View
{
    /// <summary>
    /// Used to move a transform relative to the main camera position with a scale factor applied.
    /// This is used to implement parallax scrolling effects on different branches of gameobjects.
    /// </summary>
    // Forces this LateUpdate to run after CinemachineBrain's own LateUpdate, which moves
    // the actual Camera transform this reads. Unity doesn't guarantee ordering between
    // two different scripts' LateUpdate at the default order (0), so without this a layer
    // pinned 1:1 to the camera (movementScale (1,1,0), used as a skybox) visibly jitters
    // whenever that ordering flips frame-to-frame between reading last frame's vs this
    // frame's camera position.
    [DefaultExecutionOrder(1000)]
    public class ParallaxLayer : MonoBehaviour
    {
        /// <summary>
        /// Movement of the layer is scaled by this value.
        /// </summary>
        public Vector3 movementScale = Vector3.one;

        Transform _camera;

        void Awake()
        {
            _camera = Camera.main.transform;
        }

        void LateUpdate()
        {
            transform.position = Vector3.Scale(_camera.position, movementScale);
        }

    }
}