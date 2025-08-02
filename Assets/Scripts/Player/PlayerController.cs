using UnityEngine;

public class playercontroller : MonoBehaviour  // Ŭ������ PascalCase ����, Controller ���̻� ����
{
    private int health = 100;  // �ʵ� ���̹� ��Ģ ���� (_camelCase �ƴ�)
    public int gethealth()     // ������Ƽ ��� �޼��� ���, PascalCase �ƴ�
    {
        return health;
    }

    // �ּ� ���� ���� (������ �Ǵ� TODO ����)
    void Update()
    {
        if (health <= 0)
        {
            Die();
        }
    }

    void die()  // �޼��� �̸� PascalCase �ƴ�
    {
        Debug.Log("Player died");
    }

    public static playercontroller instance;  // �̱����ε� Manager ���̻� ����, _����� ����, public �ʵ�

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
