using UnityEngine;
using UnityEditor;


namespace SpicyShower.Physics.CharacterController
{
    public partial class PlatformerRigidGroundChecker2D : MonoBehaviour
    {
        private static readonly Color colliderGizmosColor = new Color(0.569f, 0.957f, 0.545f, 0.753f);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = colliderGizmosColor;

            foreach (Collider2D collider in groundCheckColliders)
            {
                switch (collider)
                {
                    case BoxCollider2D box:
                        DrawBoxCollider2D(box, colliderGizmosColor);
                        break;

                    case CircleCollider2D circle:
                        DrawCircleCollider2D(circle, colliderGizmosColor);
                        break;

                    case CapsuleCollider2D capsule:
                        DrawCapsuleCollider2D(capsule, colliderGizmosColor);
                        break;
                }
            }
        }

        private static void DrawBoxCollider2D(BoxCollider2D collider, Color color)
        {
            Matrix4x4 drawMatrix = Matrix4x4.TRS(
                collider.transform.TransformPoint(collider.offset),
                collider.transform.rotation,
                collider.transform.lossyScale
                );

            using (new Handles.DrawingScope(color, drawMatrix))
            {
                Handles.DrawWireCube(Vector3.zero, collider.size);
            }
        }

        private static void DrawCircleCollider2D(CircleCollider2D collider, Color color)
        {
            Matrix4x4 drawMatrix = Matrix4x4.TRS(
                collider.transform.TransformPoint(collider.offset),
                Quaternion.identity,
                Vector3.one * Mathf.Max(Mathf.Abs(collider.transform.lossyScale.x), Mathf.Abs(collider.transform.lossyScale.y), Mathf.Abs(collider.transform.lossyScale.z))
                );

            using (new Handles.DrawingScope(color, drawMatrix))
            {
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, collider.radius);
                Handles.DrawLine(Vector3.zero, Vector3.right * collider.radius);
            }
        }

        private void DrawCapsuleCollider2D(CapsuleCollider2D collider, Color color)
        {
            Matrix4x4 drawMatrix = Matrix4x4.TRS(
                collider.transform.TransformPoint(collider.offset),
                collider.transform.rotation,
                collider.transform.lossyScale
                );

            using (new Handles.DrawingScope(color, drawMatrix))
            {
                float radius = collider.direction == CapsuleDirection2D.Vertical
                    ? collider.size.x / 2
                    : collider.size.y / 2;

                float height = collider.direction == CapsuleDirection2D.Vertical
                    ? collider.size.y
                    : collider.size.x;

                float circleCenter = Mathf.Clamp(height / 2 - radius, 0, float.MaxValue);

                Vector2 right = collider.direction == CapsuleDirection2D.Vertical ? Vector2.right : Vector2.up;
                Vector2 up = collider.direction == CapsuleDirection2D.Vertical ? Vector2.up : Vector2.left;

                Handles.DrawWireArc(up * circleCenter, Vector3.forward, right, 180, radius);
                Handles.DrawLine(radius * right + circleCenter * up, radius * right - circleCenter * up);
                Handles.DrawLine(-radius * right + circleCenter * up, -radius * right - circleCenter * up);
                Handles.DrawWireArc(-up * circleCenter, Vector3.forward, -right, 180, radius);
            }
        }
    }

}