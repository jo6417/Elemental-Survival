using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotInfo
{
    public SlotType slotType;
    public enum SlotType { Magic, Item };
    public int id; //고유 아이디
    public string name; // 이름
    public int grade; // 등급
    public string description; //아이템 설명
    public int price; // 가격

    // public SlotInfo(MagicInfo magicInfo)
    // {
    //     // 슬롯 타입을 마법으로 지정
    //     this.slotType = SlotType.Magic;

    //     // 나머지 정보 복사
    //     this.id = magicInfo.id;
    //     this.name = magicInfo.name;
    //     this.grade = magicInfo.grade;
    //     this.description = magicInfo.description;
    //     this.price = magicInfo.price;
    // }

    // public SlotInfo(ItemInfo itemInfo)
    // {
    //     // 슬롯 타입을 아이템으로 지정
    //     this.slotType = SlotType.Item;

    //     // 나머지 정보 복사
    //     this.id = itemInfo.id;
    //     this.name = itemInfo.name;
    //     this.grade = itemInfo.grade;
    //     this.description = itemInfo.description;
    //     this.price = itemInfo.price;
    // }
}
