#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>

// Compiled into UnityFramework for ARM64 iPhone; no credentials or entitlements.
extern "C" __attribute__((visibility("default"))) void AngelsHaptic(int signal)
{
    if(signal<1||signal>5)return;
    dispatch_async(dispatch_get_main_queue(), ^{
        if([UIApplication sharedApplication].applicationState!=UIApplicationStateActive)return;
        static UIImpactFeedbackGenerator *light;
        static UIImpactFeedbackGenerator *medium;
        static UIImpactFeedbackGenerator *heavy;
        static UINotificationFeedbackGenerator *notification;
        if(signal<=3)
        {
            if(!light)light=[[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            if(!medium)medium=[[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            if(!heavy)heavy=[[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            UIImpactFeedbackGenerator *generator=signal==1?light:signal==2?medium:heavy;
            [generator prepare];[generator impactOccurred];
        }
        else
        {
            if(!notification)notification=[[UINotificationFeedbackGenerator alloc] init];
            [notification prepare];
            [notification notificationOccurred:signal==4?UINotificationFeedbackTypeSuccess:UINotificationFeedbackTypeError];
        }
    });
}
