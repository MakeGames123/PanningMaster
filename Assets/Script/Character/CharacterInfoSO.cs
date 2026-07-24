using UnityEngine;

// 캐릭터 에셋 정보(BulletInfoSO 문법) — 시트(ChracterRoster)가 못 담는 스프라이트를 소유.
// characterId는 시트 Id(rusty/mae/…)와 1:1.
[CreateAssetMenu(fileName = "CharacterInfoSO", menuName = "Scriptable Object/CharacterInfoSO")]
public class CharacterInfoSO : ScriptableObject
{
    public string characterId;
    public Sprite characterSprite;
}
