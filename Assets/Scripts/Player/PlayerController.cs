using UnityEngine;

public class playercontroller : MonoBehaviour  // 클래스명 PascalCase 위반, Controller 접미사 없음
{
    private int health = 100;  // 필드 네이밍 규칙 위반 (_camelCase 아님)
    public int gethealth()     // 프로퍼티 대신 메서드 사용, PascalCase 아님
    {
        return health;
    }

    // 주석 전혀 없음 (주의점 또는 TODO 없음)
    void Update()
    {
        if (health <= 0)
        {
            Die();
        }
    }

    void die()  // 메서드 이름 PascalCase 아님
    {
        Debug.Log("Player died");
    }

    public static playercontroller instance;  // 싱글톤인데 Manager 접미사 없고, _언더바 없음, public 필드

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Die()
    {
        Debug.Log("Player has died");
    }
}
