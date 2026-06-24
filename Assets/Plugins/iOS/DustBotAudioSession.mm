#import <AVFoundation/AVFoundation.h>

extern "C"
{
    void DustBot_ConfigureAudioSession()
    {
        AVAudioSession *session = [AVAudioSession sharedInstance];
        NSError *categoryError = nil;
        [session setCategory:AVAudioSessionCategoryPlayback
                        mode:AVAudioSessionModeDefault
                     options:AVAudioSessionCategoryOptionMixWithOthers
                       error:&categoryError];

        NSError *activeError = nil;
        [session setActive:YES error:&activeError];

        if (categoryError != nil)
        {
            NSLog(@"DustBot audio category error: %@", categoryError);
        }

        if (activeError != nil)
        {
            NSLog(@"DustBot audio activation error: %@", activeError);
        }
    }
}
