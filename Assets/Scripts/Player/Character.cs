﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour {
	private const int HEALTH_INDEX = 0;
	private const int SPEED_INDEX = 1;
	private const int GOLD_CARRY_INDEX = 2;
	private int[,] characterStats = new int[,] { {200, 1, 5}, {75, 2, 4}, {100, 5, 300}, {150, 3, 2}, {125, 4, 1} };
	private int characterHealth;
	private int characterSpeed;
	private int goldCapacity;
	private int goldCarry;
	private int teamId;
	private int maxHealth;
	private int userId;
	private PlayerGUI gui;
	private Global earth;
	[SerializeField] private Material mat0, mat1, mat2, mat3;
	[Header("Effects")]
	[SerializeField] private GameObject bloodObject;
	private GameController gameController;
	private MeshRenderer renderer;
	NetworkView networkView;

	void Start () {
		maxHealth = characterHealth;
		gui = gameObject.GetComponentInChildren<PlayerGUI> ();
		gui.setHealth (characterHealth);
		gameController = GameObject.FindWithTag("Control").GetComponent<GameController>();
		goldCarry = 0;
		gui.setGold (goldCarry, goldCapacity);
	}

	public void setUserId(int id) {
		userId = id;
	}

	public void setClass (int reference) {
		characterHealth = characterStats [reference, HEALTH_INDEX];
		characterSpeed = characterStats [reference, SPEED_INDEX];
		goldCapacity = characterStats [reference, GOLD_CARRY_INDEX];
	}

	[RPC]
	void setHealth(int damage) {
		if (!gameObject.GetComponent<NetworkView> ().isMine) {return;}
		if (damage < 0) {
			Network.Instantiate(bloodObject, gameObject.transform.position, Quaternion.Euler(gameObject.transform.forward), 0);
		}
		characterHealth += damage;
		if (characterHealth > maxHealth) {
			characterHealth = maxHealth;
		}
		else if (characterHealth < 0) {
			characterHealth = 0;
		}
		gui.setHealth(characterHealth);
		if (characterHealth == 0) {
			getDead ();
		}
	}
	void getDead() {
		Network.Destroy(gameObject);
		gameController.sendPlayerDeathRPC(userId);
		gameController.spawnPlayer();
	}

	public float getSpeed() {
		return characterSpeed;
	}

	public int getGoldCapacity() {
		return goldCapacity;
	}

	public int getTeamId() {
		return teamId;
	}

	public void setTeamId(int id){
		teamId = id;
		networkView = gameObject.GetComponent<NetworkView> ();
		networkView.RPC ("setCharacterMaterial", RPCMode.All, id);
	}
	public void setGoldCarry(int gold) {
		goldCarry = gold;
		gui.setGold (goldCarry, goldCapacity);
	}

	public int getGoldCarry() {
		return goldCarry;
	}

	void OnTriggerStay (Collider col) {
		if (col.gameObject.tag == "Mine Cart") {
			Minecart cart = col.gameObject.GetComponentInParent<Minecart> ();
			int cartId = cart.getTeamId();
			if (cartId != teamId && goldCarry < goldCapacity && cart.getGold() > 0) {
				gui.setInteract ("Press F to Steal Gold");
				if (Input.GetKey (KeyCode.F)) {
					int amount = Mathf.Min(goldCapacity - goldCarry, cart.getGold());
					gameController.sendCartGoldRPC(cartId, -amount);
					networkView.RPC("setGoldCarryRPC", RPCMode.All, amount);
					gui.setInteract ("");
				}
			}
			else if (cartId == teamId && goldCarry > 0) {
				gui.setInteract ("Press F to Place Gold");
				if (Input.GetKey (KeyCode.F)) {
					gameController.sendCartGoldRPC(cartId, goldCarry);
					networkView.RPC("setGoldCarryRPC", RPCMode.All, -goldCarry);
					gui.setInteract ("");
				}
			}
		}
	}

	void OnTriggerExit (Collider col) {
		if (col.gameObject.tag == "Mine Cart") {
			gui.setInteract ("");
		}
	}
	[RPC]
	void setCharacterMaterial(int id) {
		renderer = gameObject.GetComponent<MeshRenderer> ();
		switch (id) {
		case(0):
			renderer.material = mat0;
			break;
		case(1):
			renderer.material = mat1;
			break;
		case(2):
			renderer.material = mat2;
			break;
		case(3):
			renderer.material = mat3;
			break;
		default:
			break;
		}
	}
	[RPC]
	void setGoldCarryRPC (int amount) {
		setGoldCarry(goldCarry + amount);
	}
}
