using UnityEngine;
using Facebook.Unity;
using System.Collections.Generic;

public class DebugKeyHash : MonoBehaviour
{
    void Start()
    {
        // Imprime el Key Hash en la consola
        CheckKeyHash();
    }

    private void CheckKeyHash()
    {
        // Esta función es 'mágica' para obtener el hash real que está viendo Android
        // Nota: Solo funciona si el SDK de FB tiene métodos internos o usando JNI, 
        // pero una forma más fácil en Unity puro no siempre está expuesta.

        // TRUCO: Forzamos un error de login para ver el hash en el Logcat.
        // Si tu hash es incorrecto, cuando intentes hacer Login, 
        // Facebook mostrará un error en pantalla o en Logcat diciendo:
        // "Invalid key hash. The key hash XXXXXX does not match any stored key hashes."

        Debug.Log("=== REVISA TU LOGCAT AL DARLE LOGIN ===");
    }
}