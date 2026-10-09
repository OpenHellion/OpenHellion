using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using POpusCodec;
using POpusCodec.Enums;
using Steamworks;
using UnityEngine;
using ZeroGravity.LevelDesign;
using ZeroGravity.Network;
using ZeroGravity.Objects;
using OpenHellion.Social.RichPresence;
using OpenHellion.IO;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;

namespace ZeroGravity.Audio
{
	public class VoiceCommTransmitter : MonoBehaviour
	{
		private AudioClip _inAudioClip;

		private string _micDevice;

		private const int SampleRate = 48000;

		private bool _talk;

		private bool _radio;

		private OpusEncoder _opusEncoder;

		private int _lastSample;

		[SerializeField, FormerlySerializedAs("maxRadioDistance")]
		private float _maxRadioDistance = 1000f;

		private void Start()
		{
			_opusEncoder = new OpusEncoder(SamplingRate.Sampling48000, Channels.Mono, 56000, OpusApplicationType.Voip,
				Delay.Delay40ms);
		}

		private void Update()
		{
			if (!RichPresenceManager.HasSteam)
			{
				return;
			}

			if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Talk) && !_talk)
			{
				if (Microphone.devices.Length > 0)
				{
					if (MyPlayer.IsAudioDebug)
					{
						Debug.Log("Start " + AudioListener.volume);
					}

					_micDevice = Microphone.devices[0];
					_inAudioClip = Microphone.Start(_micDevice, loop: true, 5, SampleRate);
					_lastSample = 0;
					_radio = false;
					_talk = true;
				}
			}
			else if (ControlsSubsystem.GetButtonDown(ControlsSubsystem.ConfigAction.Radio) && !_talk)
			{
				if (Microphone.devices.Length > 0)
				{
					_micDevice = Microphone.devices[0];
					_inAudioClip = Microphone.Start(_micDevice, loop: true, 5, SampleRate);
					_lastSample = 0;
					_radio = true;
					_talk = true;
				}
			}
			else if ((ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Talk) ||
			          ControlsSubsystem.GetButtonUp(ControlsSubsystem.ConfigAction.Radio)) && _talk)
			{
				if (MyPlayer.IsAudioDebug)
				{
					Debug.Log("Stop " + AudioListener.volume);
				}

				_inAudioClip = null;
				_talk = false;
			}
		}

		private async UniTaskVoid FixedUpdate()
		{
			if (!RichPresenceManager.HasSteam)
			{
				return;
			}

			SceneTriggerRoom currentRoomTrigger = MyPlayer.Instance.CurrentRoomTrigger;
			Helmet currentHelmet = MyPlayer.Instance.CurrentHelmet;
			if (!_talk || _inAudioClip == null ||
			    ((currentRoomTrigger == null || !(currentRoomTrigger.AirPressure > 0f)) &&
			     (currentHelmet == null || !currentHelmet.IsVisorActive)))
			{
				return;
			}

			int num = -1;
			List<byte[]> list = new List<byte[]>();
			while (true)
			{
				int num2 = Microphone.GetPosition(_micDevice);
				if (num2 == num)
				{
					break;
				}

				if (num2 < _lastSample)
				{
					num2 += _inAudioClip.samples;
				}

				int num3 = num2 - _lastSample;
				int num4 = _opusEncoder.FrameSizePerChannel * _inAudioClip.channels;
				if (num3 >= num4)
				{
					float[] array = new float[_opusEncoder.FrameSizePerChannel * _inAudioClip.channels];
					if (_lastSample + num4 > _inAudioClip.samples)
					{
						float[] array2 = new float[_inAudioClip.samples - _lastSample];
						float[] array3 = new float[num4 - (_inAudioClip.samples - _lastSample)];
						if (array2.Length > 0)
						{
							_inAudioClip.GetData(array2, _lastSample);
							Array.Copy(array2, 0, array, array3.Length, array2.Length);
						}

						_inAudioClip.GetData(array3, 0);
						Array.Copy(array3, 0, array, 0, array3.Length);
					}
					else
					{
						_inAudioClip.GetData(array, _lastSample);
					}

					_lastSample += _opusEncoder.FrameSizePerChannel * _inAudioClip.channels;
					if (_lastSample > _inAudioClip.samples)
					{
						_lastSample %= _inAudioClip.samples;
					}

					ArraySegment<byte> arraySegment = _opusEncoder.Encode(array);
					byte[] array4 = new byte[arraySegment.Count];
					Array.Copy(arraySegment.Array, array4, arraySegment.Count);
					list.Add(array4);
				}

				num = num2;
			}

			if (list.Count > 0)
			{
				VoiceCommDataMessage voiceCommDataMessage = new VoiceCommDataMessage
				{
					SourceGUID = MyPlayer.Instance.Guid,
					IsRadioComm = _radio,
					AudioPackets = list
				};
				byte[] msgBytes = await ProtoSerialiser.Pack(voiceCommDataMessage);
				HashSet<OtherPlayer> hashSet = new HashSet<OtherPlayer>();
				hashSet.UnionWith(GetAllPlayersFromSameVessel());
				if (_radio)
				{
					hashSet.UnionWith(GetAllPlayersWithinRadius(transform.position, _maxRadioDistance));
				}

				SendP2PPacketToPlayers(hashSet, msgBytes);
			}
		}

		private static IEnumerable<OtherPlayer> GetAllPlayersFromSameVessel()
		{
			if (MyPlayer.Instance.Parent is SpaceObjectVessel)
			{
				SpaceObjectVessel mainVessel = (MyPlayer.Instance.Parent as SpaceObjectVessel).MainVessel;
				return mainVessel.GetComponentsInChildren<OtherPlayer>();
			}

			return new OtherPlayer[0];
		}

		private static IEnumerable<OtherPlayer> GetAllPlayersWithinRadius(Vector3 position, float radius)
		{
			float num = radius * radius;
			HashSet<OtherPlayer> hashSet = new HashSet<OtherPlayer>();
			foreach (OtherPlayer value in MyPlayer.Instance.World.AllPlayers)
			{
				if ((value.transform.position - position).sqrMagnitude < num)
				{
					hashSet.Add(value);
				}
			}

			return hashSet;
		}

		private static void SendP2PPacketToPlayers(ICollection<OtherPlayer> otherPlayers, byte[] msgBytes)
		{
			new Thread((ThreadStart)delegate
			{
				// Create int pointer to copy the data to.
				IntPtr msg = IntPtr.Zero;
				Marshal.Copy(msgBytes, 0, msg, msgBytes.Length);

				foreach (OtherPlayer otherPlayer in otherPlayers)
				{
					try
					{
						// Get player's steam identity.
						SteamNetworkingIdentity sni = new SteamNetworkingIdentity();
						//sni.SetSteamID(new CSteamID(ulong.Parse(otherPlayer.NativeId)));

						// https://partner.steamgames.com/doc/api/steamnetworkingtypes#message_sending_flags
						SteamNetworkingMessages.SendMessageToUser(ref sni, msg, (uint)msgBytes.Length, 8, 0);
					}
					catch
					{
						Debug.LogError("Couldn't send audio packet.");
					}
				}
			}).Start();
		}
	}
}
