#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenHellion
{
	public class DiagnosticsOverlay : MonoBehaviour
	{
		private const float FlashSeconds = 2f;
		private const float StaleSeconds = 1f;
		private const float Margin = 10f;

		private bool _visible;
		private Diagnostics.DiagnosticsState _state;
		private readonly string[] _rows = new string[Diagnostics.RowCount];
		private readonly float[] _changedAt = new float[Diagnostics.RowCount];
		private GUIStyle _style;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
			GameObject overlay = new GameObject(nameof(DiagnosticsOverlay));
			DontDestroyOnLoad(overlay);
			overlay.AddComponent<DiagnosticsOverlay>();
		}

		private void Update()
		{
			if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
			{
				_visible = !_visible;
			}

			bool hadState = _state != null;
			Diagnostics.TryGather(out _state);
			if (_state == null)
			{
				return;
			}

			float now = Time.unscaledTime;
			Diagnostics.Assert(!hadState || Diagnostics.Row(_state, Diagnostics.GravityRow) == _rows[Diagnostics.GravityRow] ||
				Diagnostics.Row(_state, Diagnostics.RoomRow) != _rows[Diagnostics.RoomRow],
				$"gravity changed without a room change: {_rows[Diagnostics.GravityRow]} -> {Diagnostics.Row(_state, Diagnostics.GravityRow)}");
			for (int row = 0; row < Diagnostics.RowCount; row++)
			{
				string text = Diagnostics.Row(_state, row);
				if (hadState && text == _rows[row])
				{
					continue;
				}

				_rows[row] = text;
				_changedAt[row] = now;
				if (!hadState || Diagnostics.IsLogged(row))
				{
					Diagnostics.Log(text);
				}
			}
		}

		private void OnGUI()
		{
			if (!_visible || _state == null)
			{
				return;
			}

			_style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
			float line = _style.lineHeight + 2f;
			float now = Time.unscaledTime;

			DrawGroup("player", Diagnostics.ParentRow, Diagnostics.ParentHealthRow, false, Margin);
			float parentTop = DrawGroup("network", Diagnostics.LatencyRow, Diagnostics.ObjectsRow, true, Margin);
			float roomTop = DrawGroup("parent object", Diagnostics.ParentHealthRow, Diagnostics.RoomAirRow, true, parentTop + line);
			DrawGroup("room", Diagnostics.RoomAirRow, Diagnostics.LatencyRow, true, roomTop + line);
			DrawGroup("objects", Diagnostics.ObjectsRow, Diagnostics.RowCount, false, Screen.height - Margin - 2f * line);
			GUI.color = Color.white;
			_style.alignment = TextAnchor.UpperLeft;

			float DrawGroup(string title, int first, int end, bool alignRight, float top)
			{
				float width = _style.CalcSize(new GUIContent(title)).x;
				for (int row = first; row < end; row++)
				{
					width = Mathf.Max(width, _style.CalcSize(new GUIContent(_rows[row])).x);
				}

				_style.alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
				float x = alignRight ? Screen.width - Margin - width - 4f : Margin;
				DrawLabel(x, top, width, title, Color.cyan);
				for (int row = first; row < end; row++)
				{
					top += line;
					Color colour = Diagnostics.IsInvalid(_state, row) ? Color.red
						: Diagnostics.IsLogged(row) && now - _changedAt[row] < FlashSeconds && Mathf.Repeat(now * 4f, 1f) < 0.5f ? Color.yellow
						: row == Diagnostics.MessagesRow && now - _changedAt[row] > StaleSeconds ? Color.grey
						: Color.white;
					DrawLabel(x, top, width, _rows[row], colour);
				}

				return top + line;
			}

			void DrawLabel(float x, float y, float width, string text, Color colour)
			{
				GUI.color = Color.black;
				GUI.Label(new Rect(x + 1f, y + 1f, width + 4f, line), text, _style);
				GUI.color = colour;
				GUI.Label(new Rect(x, y, width + 4f, line), text, _style);
			}
		}
	}
}
#endif
