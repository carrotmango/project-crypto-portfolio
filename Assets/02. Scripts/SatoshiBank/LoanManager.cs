//using UnityEngine;

//public class LoanManager : MonoBehaviour {
//    public double loanAmount = 30000000000; //
//    public double interestRate = 0.0819; // 8.19%
//    private bool loanActive = false;
//    private float monthTimer = 0f;
//    private float secondsPerMonth = 10f; //

//    public void Loan() {
//        PlayerManager.Instance.satoshiBankCash += loanAmount;
//        loanActive = true;
//    }

//    void Update() {
//        if (!loanActive) return;

//        monthTimer += Time.deltaTime;
//        if (monthTimer >= secondsPerMonth) {
//            monthTimer = 0f;
//            ApplyMonthlyInterest();
//        }
//    }

//    void ApplyMonthlyInterest() {
//        double monthlyRate = interestRate / 12.0;
//        double interest = loanAmount * monthlyRate;
//        PlayerManager.Instance.satoshiBankCash -= interest;

//        Debug.Log($"[����] {interest:N0}�� ���� ������. ���� �ܾ�: {PlayerManager.Instance.satoshiBankCash:N0}");

//        CoinManager.Instance?.UpdateCashText();
//    }

    
//    public void OnLoanButtonClicked() {
//        /*Loan(); */

//        if (UIManager.Instance != null) {
//            Debug.Log("대출");
//            UIManager.Instance.ShowConfirm("현재는 대출이 불가능해요!");
//        } else {
//            Debug.Log("대출X");
//        }
//    }
//}
